"""
save_orders_fabric.py
=====================
Microsoft Fabric — SaveOrders ETL  (2016 – 2028)
Equivalent of: BCAppCode/BHG-DR-LIB/SaveOrders.cs

This single file contains:
  - Shared connection helpers and type converters
  - extract_source_orders()   — SELECT from SAMMS SQL Server (source)
  - run_save_orders_year()    — Core upsert engine (SQL MERGE on destination)
  - save_orders_2016() … save_orders_2028()  — Named entry points per year
  - run_all_years_for_site()  — Orchestrator: loops 2016-2028 with failure gate
  - run_all_sites()           — Full run across every active clinic

How the upsert works (mirrors the C# EF Core logic exactly)
------------------------------------------------------------
  1. Extract source rows for the year from SAMMS — CHECKSUM pre-computed at source.
  2. Bulk-insert into a temp staging table (#OrdersStaging) on Azure SQL.
  3. Single T-SQL MERGE:
       NOT MATCHED BY TARGET       → INSERT  (new order)
       MATCHED, checksum changed   → UPDATE all columns
       MATCHED, checksum same      → touch RowState=1, LastModAt only
       NOT MATCHED BY SOURCE       → soft-delete: RowState=0, Active=0
         (orders that disappeared from the source extract for this site)

Prerequisites
-------------
  pip install pyodbc pandas

Usage — Fabric Notebook
-----------------------
  # Set connection strings (use notebookutils.credentials.getSecret in production)
  DEST_CONSTR = "<Azure BHG_DR connection string>"
  SITE_CODE   = "B01"

  # Option A — run one year
  from save_orders_fabric import save_orders_2024
  save_orders_2024(SITE_CODE, "<SAMMS connection string>", DEST_CONSTR)

  # Option B — run all years for one site
  from save_orders_fabric import run_all_years_for_site
  run_all_years_for_site(SITE_CODE, DEST_CONSTR)

  # Option C — run all sites, all years
  from save_orders_fabric import run_all_sites
  run_all_sites(DEST_CONSTR)
"""

from __future__ import annotations

from datetime import datetime
from typing import Optional

import pandas as pd
import pyodbc

# =============================================================================
# SECTION 1 — TYPE CONVERSION HELPERS
# =============================================================================

def _parse_dt(val) -> Optional[datetime]:
    s = str(val).strip() if val is not None else ""
    if not s or s.lower() in ("nat", "none", "null", "nan", ""):
        return None
    try:
        return pd.Timestamp(s).to_pydatetime()
    except Exception:
        return None


def _parse_bool(val) -> bool:
    if isinstance(val, bool):
        return val
    if val is None or (isinstance(val, float) and pd.isna(val)):
        return False
    return str(val).strip().lower() in ("true", "1", "yes")


def _parse_bool_nullable(val) -> Optional[bool]:
    s = str(val).strip() if val is not None else ""
    if not s or s.lower() in ("none", "null", "nan", ""):
        return None
    return s.lower() in ("true", "1", "yes")


def _parse_int(val, default: Optional[int] = None) -> Optional[int]:
    s = str(val).strip() if val is not None else ""
    if not s or s.lower() in ("none", "null", "nan", ""):
        return default
    try:
        return int(float(s))
    except Exception:
        return default


def _parse_decimal(val, default=None):
    s = str(val).strip() if val is not None else ""
    if not s or s.lower() in ("none", "null", "nan", ""):
        return default
    try:
        return float(s)
    except Exception:
        return default


def _safe_str(val) -> str:
    if val is None or (isinstance(val, float) and pd.isna(val)):
        return ""
    return str(val)


def _to_bytes(val) -> bytes:
    if val is None or (isinstance(val, float) and pd.isna(val)):
        return b""
    if isinstance(val, (bytes, bytearray)):
        return bytes(val)
    return str(val).encode("ascii", errors="replace")


# =============================================================================
# SECTION 2 — SOURCE EXTRACTION
# =============================================================================

_SOURCE_SQL = """
SELECT
    OrderNum,       cltid,          medtype,        dateadded,
    orderdate,      doctor,         effectivedate,  expirationdate,
    dose,           dose2,          changeby,       intervals,
    sunday,         monday,         tuesday,        wednesday,
    thursday,       friday,         saturday,
    sunday2,        monday2,        tuesday2,       wednesday2,
    thursday2,      friday2,        saturday2,
    notes,          active,         type,           stype,
    weeknum,        splitfirst,     blind,          o_user,
    cltM4id,        newdose,        pckcode,        rxhistid,
    ex,             actbydate,      actbyuser,      white,
    repoldorder,    sigdr,          dtsig,          aws,
    blsched,        blverbal,       color,          deactbydate,
    deactbyuser,    ordertypev5,    sigentered,     signoted,
    signoteddt,     dtmid,          sigmid,         overapprove,
    overapprovedt,  sigentereddt,   sigdrimg,       SigMidImg,
    SigNotedImg,
    CHECKSUM(
        OrderNum, cltid, medtype, dateadded, orderdate, doctor,
        effectivedate, expirationdate, dose, dose2, changeby,
        intervals, sunday, monday, tuesday, wednesday, thursday, friday, saturday,
        sunday2, monday2, tuesday2, wednesday2, thursday2, friday2, saturday2,
        notes, active, type, stype, weeknum, splitfirst, blind,
        o_user, cltM4id, newdose, pckcode, rxhistid, ex,
        actbydate, actbyuser, white, repoldorder, sigdr, dtsig,
        aws, blsched, blverbal, color, deactbydate, deactbyuser,
        ordertypev5, sigentered, signoted, signoteddt, dtmid, sigmid,
        overapprove, overapprovedt, sigentereddt, sigdrimg, SigMidImg, SigNotedImg
    ) AS rowchksum
FROM {from_tbl_vw}
WHERE ({where_cond})
  AND YEAR(orderdate) = {year}
  AND cltid > 0
"""


def extract_source_orders(
    src_constr: str,
    from_tbl_vw: str,
    where_cond: str,
    year: int,
) -> pd.DataFrame:
    """Pull all orders for *year* from a clinic SAMMS SQL Server."""
    sql = _SOURCE_SQL.format(
        from_tbl_vw=from_tbl_vw,
        where_cond=where_cond,
        year=year,
    )
    with pyodbc.connect(src_constr) as conn:
        return pd.read_sql(sql, conn)


# =============================================================================
# SECTION 3 — STAGING TABLE + BULK INSERT
# =============================================================================

_CREATE_STAGING_SQL = """
IF OBJECT_ID('tempdb..#OrdersStaging') IS NOT NULL DROP TABLE #OrdersStaging;
CREATE TABLE #OrdersStaging (
    SiteCode       varchar(10)    NOT NULL,
    OrderNum       int            NOT NULL,
    CltId          int            NOT NULL,
    RowChkSum      int            NOT NULL,
    MedType        varchar(50)        NULL,
    DateAdded      datetime           NULL,
    Orderdate      datetime           NULL,
    Doctor         varchar(50)        NULL,
    EffectiveDate  datetime           NULL,
    ExpirationDate datetime           NULL,
    Dose           decimal(18,4)      NULL,
    Dose2          decimal(18,4)      NULL,
    Changeby       int                NULL,
    Intervals      smallint           NULL,
    Sunday         bit                NULL,  Monday    bit  NULL,
    Tuesday        bit                NULL,  Wednesday bit  NULL,
    Thursday       bit                NULL,  Friday    bit  NULL,
    Saturday       bit                NULL,
    Sunday2        bit                NULL,  Monday2   bit  NULL,
    Tuesday2       bit                NULL,  Wednesday2 bit NULL,
    Thursday2      bit                NULL,  Friday2   bit  NULL,
    Saturday2      bit                NULL,
    Notes          varchar(1000)      NULL,
    Active         bit                NULL,
    Type           varchar(50)        NULL,
    Stype          varchar(50)        NULL,
    Weeknum        int                NULL,
    SplitFirst     bit                NULL,
    Blind          bit                NULL,
    OUser          varchar(100)       NULL,
    CltM4id        varchar(50)        NULL,
    Newdose        int                NULL,
    Pckcode        varchar(50)        NULL,
    RxhistId       varchar(50)        NULL,
    Ex             bit                NULL,
    ActbyDate      datetime           NULL,
    ActByUser      varchar(100)       NULL,
    White          bit                NULL,
    RepOldOrder    decimal(18,4)      NULL,
    SigDr          nvarchar(max)      NULL,
    DtSig          datetime           NULL,
    Aws            bit                NULL,
    BlSched        bit                NULL,
    BlVerbal       bit                NULL,
    Color          varchar(50)        NULL,
    DeActbyDate    datetime           NULL,
    DeActbyUser    varchar(100)       NULL,
    OrderTypev5    varchar(50)        NULL,
    Sigentered     nvarchar(max)      NULL,
    Signoted       nvarchar(max)      NULL,
    SigNoteddt     datetime           NULL,
    Dtmid          datetime           NULL,
    SigMid         nvarchar(max)      NULL,
    OverApprove    varchar(100)       NULL,
    OverapproveDt  varchar(100)       NULL,
    Sigentereddt   datetime           NULL,
    SigDrImg       varbinary(max)     NULL,
    SigMidImg      varbinary(max)     NULL,
    SigNotedImg    varbinary(max)     NULL
);
"""

_STAGING_INSERT_SQL = (
    "INSERT INTO #OrdersStaging ("
    "SiteCode, OrderNum, CltId, RowChkSum, "
    "MedType, DateAdded, Orderdate, Doctor, EffectiveDate, ExpirationDate, "
    "Dose, Dose2, Changeby, Intervals, "
    "Sunday, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, "
    "Sunday2, Monday2, Tuesday2, Wednesday2, Thursday2, Friday2, Saturday2, "
    "Notes, Active, Type, Stype, Weeknum, SplitFirst, Blind, "
    "OUser, CltM4id, Newdose, Pckcode, RxhistId, Ex, "
    "ActbyDate, ActByUser, White, RepOldOrder, SigDr, DtSig, "
    "Aws, BlSched, BlVerbal, Color, DeActbyDate, DeActbyUser, "
    "OrderTypev5, Sigentered, Signoted, SigNoteddt, Dtmid, SigMid, "
    "OverApprove, OverapproveDt, Sigentereddt, SigDrImg, SigMidImg, SigNotedImg"
    ") VALUES (" + ",".join(["?"] * 64) + ")"
)


def _build_staging_rows(site_code: str, src_df: pd.DataFrame) -> list[tuple]:
    """
    Convert source DataFrame rows into parameter tuples for #OrdersStaging.
    Applies all nullable/column-existence guards from the C# methods,
    including the 1900-01-01 sentinel for dtmid and Notes truncation.
    """
    cols = set(src_df.columns.str.lower())
    has = lambda c: c in cols   # noqa: E731  (column-existence guard)

    rows: list[tuple] = []
    for _, r in src_df.iterrows():
        # Notes truncation (guard added in SaveOrders2028, applied here for all years)
        notes = _safe_str(r.get("notes"))
        if len(notes) > 1000:
            notes = notes[:999].strip()

        # dtmid sentinel: '1900-01-01 …' stored as NULL in destination
        dtmid_str = _safe_str(r.get("dtmid"))
        dtmid_val = (
            None
            if (not dtmid_str or dtmid_str.startswith("1900-01-01"))
            else _parse_dt(dtmid_str)
        )

        rows.append((
            site_code,
            int(r["OrderNum"]),
            int(r["cltid"]),
            int(r["rowchksum"]),
            # core fields
            _safe_str(r.get("medtype")),
            _parse_dt(r.get("dateadded")),
            _parse_dt(r.get("orderdate")),
            _safe_str(r.get("doctor")),
            _parse_dt(r.get("effectivedate")),
            _parse_dt(r.get("expirationdate")),
            _parse_decimal(r.get("dose"),  0.0),
            _parse_decimal(r.get("dose2"), 0.0),
            _parse_int(r.get("changeby"),  0),
            _parse_int(r.get("intervals"), 0),
            # day-of-week flags
            _parse_bool(r.get("sunday")),   _parse_bool(r.get("monday")),
            _parse_bool(r.get("tuesday")),  _parse_bool(r.get("wednesday")),
            _parse_bool(r.get("thursday")), _parse_bool(r.get("friday")),
            _parse_bool(r.get("saturday")),
            _parse_bool(r.get("sunday2")),  _parse_bool(r.get("monday2")),
            _parse_bool(r.get("tuesday2")), _parse_bool(r.get("wednesday2")),
            _parse_bool(r.get("thursday2")),_parse_bool(r.get("friday2")),
            _parse_bool(r.get("saturday2")),
            # misc
            notes,
            _parse_bool(r.get("active")),
            _safe_str(r.get("type")),
            _safe_str(r.get("stype")),
            _parse_int(r.get("weeknum"), 0),
            _parse_bool(r.get("splitfirst")),
            _parse_bool(r.get("blind")),
            _safe_str(r.get("o_user")),
            _safe_str(r.get("cltM4id")),
            _parse_int(r.get("newdose")),               # nullable int
            _safe_str(r.get("pckcode")),
            _safe_str(r.get("rxhistid")),
            _parse_bool_nullable(r.get("ex")),           # nullable bit
            _parse_dt(r.get("actbydate")),
            _safe_str(r.get("actbyuser")),
            _parse_bool_nullable(r.get("white")),        # nullable bit
            _parse_decimal(r.get("repoldorder")),        # nullable decimal
            # sig fields — column-existence guarded (not present on all SAMMS versions)
            _safe_str(r.get("sigdr"))        if has("sigdr")        else "",
            _parse_dt(r.get("dtsig")),
            _parse_bool_nullable(r.get("aws")),
            _parse_bool_nullable(r.get("blsched")),
            _parse_bool_nullable(r.get("blverbal")),
            _safe_str(r.get("color")),
            _parse_dt(r.get("deactbydate")),
            _safe_str(r.get("deactbyuser")),
            _safe_str(r.get("ordertypev5")),
            _safe_str(r.get("sigentered"))   if has("sigentered")   else "",
            _safe_str(r.get("signoted"))     if has("signoted")     else "",
            _parse_dt(r.get("signoteddt")),
            dtmid_val,
            _safe_str(r.get("sigmid"))       if has("sigmid")       else "",
            _safe_str(r.get("overapprove")),
            _safe_str(r.get("overapprovedt")),
            _parse_dt(r.get("sigentereddt")) if has("sigentereddt") else None,
            _to_bytes(r.get("sigdrimg"))     if has("sigdrimg")     else b"",
            _to_bytes(r.get("SigMidImg"))    if has("sigmidimg")    else b"",
            _to_bytes(r.get("SigNotedImg"))  if has("signotedimg")  else b"",
        ))
    return rows


# =============================================================================
# SECTION 4 — T-SQL MERGE STATEMENT
# =============================================================================

def _build_merge_sql(target_table: str, site_code: str) -> str:
    """
    Build the MERGE that replicates the SaveOrders20XX EF Core upsert logic.

    WHEN NOT MATCHED BY TARGET          → INSERT  (new row for this site)
    WHEN MATCHED, checksum changed      → full UPDATE of all columns
    WHEN MATCHED, checksum unchanged    → touch RowState=1, LastModAt only
    WHEN NOT MATCHED BY SOURCE          → soft-delete: RowState=0, Active=0
      (rows that exist in the destination but are no longer in the source
       extract for this site — mirrors the pre-loop mark-all-inactive in C#)
    """
    full_update = """\
        dst.RowChkSum      = src.RowChkSum,    dst.RowState       = 1,
        dst.LastModAt      = GETDATE(),        dst.MedType        = src.MedType,
        dst.DateAdded      = src.DateAdded,    dst.Orderdate      = src.Orderdate,
        dst.Doctor         = src.Doctor,       dst.EffectiveDate  = src.EffectiveDate,
        dst.ExpirationDate = src.ExpirationDate,
        dst.Dose           = src.Dose,         dst.Dose2          = src.Dose2,
        dst.Changeby       = src.Changeby,     dst.Intervals      = src.Intervals,
        dst.Sunday         = src.Sunday,       dst.Monday         = src.Monday,
        dst.Tuesday        = src.Tuesday,      dst.Wednesday      = src.Wednesday,
        dst.Thursday       = src.Thursday,     dst.Friday         = src.Friday,
        dst.Saturday       = src.Saturday,     dst.Sunday2        = src.Sunday2,
        dst.Monday2        = src.Monday2,      dst.Tuesday2       = src.Tuesday2,
        dst.Wednesday2     = src.Wednesday2,   dst.Thursday2      = src.Thursday2,
        dst.Friday2        = src.Friday2,      dst.Saturday2      = src.Saturday2,
        dst.Notes          = src.Notes,        dst.Active         = src.Active,
        dst.Type           = src.Type,         dst.Stype          = src.Stype,
        dst.Weeknum        = src.Weeknum,      dst.SplitFirst     = src.SplitFirst,
        dst.Blind          = src.Blind,        dst.OUser          = src.OUser,
        dst.CltM4id        = src.CltM4id,      dst.Newdose        = src.Newdose,
        dst.Pckcode        = src.Pckcode,      dst.RxhistId       = src.RxhistId,
        dst.Ex             = src.Ex,           dst.ActbyDate      = src.ActbyDate,
        dst.ActByUser      = src.ActByUser,    dst.White          = src.White,
        dst.RepOldOrder    = src.RepOldOrder,  dst.SigDr          = src.SigDr,
        dst.DtSig          = src.DtSig,        dst.Aws            = src.Aws,
        dst.BlSched        = src.BlSched,      dst.BlVerbal       = src.BlVerbal,
        dst.Color          = src.Color,        dst.DeActbyDate    = src.DeActbyDate,
        dst.DeActbyUser    = src.DeActbyUser,  dst.OrderTypev5    = src.OrderTypev5,
        dst.Sigentered     = src.Sigentered,   dst.Signoted       = src.Signoted,
        dst.SigNoteddt     = src.SigNoteddt,   dst.Dtmid          = src.Dtmid,
        dst.SigMid         = src.SigMid,       dst.OverApprove    = src.OverApprove,
        dst.OverapproveDt  = src.OverapproveDt,dst.Sigentereddt   = src.Sigentereddt,
        dst.SigDrImg       = src.SigDrImg,     dst.SigMidImg      = src.SigMidImg,
        dst.SigNotedImg    = src.SigNotedImg"""

    return f"""
MERGE {target_table} AS dst
USING #OrdersStaging AS src
   ON  dst.SiteCode = src.SiteCode
   AND dst.OrderNum = src.OrderNum
   AND dst.CltId    = src.CltId

WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        SiteCode, OrderNum, CltId, RowChkSum, RowState, LastModAt,
        MedType, DateAdded, Orderdate, Doctor, EffectiveDate, ExpirationDate,
        Dose, Dose2, Changeby, Intervals,
        Sunday, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday,
        Sunday2, Monday2, Tuesday2, Wednesday2, Thursday2, Friday2, Saturday2,
        Notes, Active, Type, Stype, Weeknum, SplitFirst, Blind,
        OUser, CltM4id, Newdose, Pckcode, RxhistId, Ex,
        ActbyDate, ActByUser, White, RepOldOrder, SigDr, DtSig,
        Aws, BlSched, BlVerbal, Color, DeActbyDate, DeActbyUser,
        OrderTypev5, Sigentered, Signoted, SigNoteddt, Dtmid, SigMid,
        OverApprove, OverapproveDt, Sigentereddt, SigDrImg, SigMidImg, SigNotedImg
    )
    VALUES (
        src.SiteCode, src.OrderNum, src.CltId, src.RowChkSum, 1, GETDATE(),
        src.MedType, src.DateAdded, src.Orderdate, src.Doctor,
        src.EffectiveDate, src.ExpirationDate,
        src.Dose, src.Dose2, src.Changeby, src.Intervals,
        src.Sunday, src.Monday, src.Tuesday, src.Wednesday,
        src.Thursday, src.Friday, src.Saturday,
        src.Sunday2, src.Monday2, src.Tuesday2, src.Wednesday2,
        src.Thursday2, src.Friday2, src.Saturday2,
        src.Notes, src.Active, src.Type, src.Stype, src.Weeknum,
        src.SplitFirst, src.Blind, src.OUser, src.CltM4id,
        src.Newdose, src.Pckcode, src.RxhistId, src.Ex,
        src.ActbyDate, src.ActByUser, src.White, src.RepOldOrder,
        src.SigDr, src.DtSig, src.Aws, src.BlSched, src.BlVerbal,
        src.Color, src.DeActbyDate, src.DeActbyUser, src.OrderTypev5,
        src.Sigentered, src.Signoted, src.SigNoteddt, src.Dtmid, src.SigMid,
        src.OverApprove, src.OverapproveDt, src.Sigentereddt,
        src.SigDrImg, src.SigMidImg, src.SigNotedImg
    )

WHEN MATCHED AND dst.RowChkSum <> src.RowChkSum THEN
    UPDATE SET {full_update}

WHEN MATCHED THEN
    UPDATE SET dst.RowState = 1, dst.LastModAt = GETDATE()

WHEN NOT MATCHED BY SOURCE AND dst.SiteCode = '{site_code}' THEN
    UPDATE SET dst.RowState = 0, dst.Active = 0

OUTPUT $action AS MergeAction,
       COALESCE(inserted.OrderNum, deleted.OrderNum) AS OrderNum;
"""


# =============================================================================
# SECTION 5 — CORE UPSERT ENGINE
# =============================================================================

def run_save_orders_year(
    year: int,
    target_table: str,
    site_code: str,
    src_constr: str,
    dest_constr: str,
    from_tbl_vw: str = "dbo.tblOrder",
    where_cond: str = "1=1",
    batch_size: int = 2000,
    verbose: bool = True,
) -> bool:
    """
    Core upsert engine — equivalent of SaveOrders20XX() in SaveOrders.cs.

    Parameters
    ----------
    year          : Calendar year (2016–2028).
    target_table  : Destination table e.g. 'pats.tbl_Orders2022'.
    site_code     : Clinic site code e.g. 'B01'.
    src_constr    : ODBC connection string for the SAMMS SQL Server.
    dest_constr   : ODBC connection string for Azure BHG_DR.
    from_tbl_vw   : Source table/view on SAMMS (from dms.vw_MapAction).
    where_cond    : WHERE clause from tsk.tbl_Tasks2.WhereCondition.
    batch_size    : Rows per staging batch (pyodbc executemany).
    verbose       : Print progress messages.

    Returns True on success, False on any exception
    (mirrors the C# bool return used by the failure gate in BHGTaskRunner).
    """
    tag = f"[SaveOrders{year}][{site_code}]"

    def log(msg: str) -> None:
        if verbose:
            print(f"{tag} {msg}")

    try:
        # 1. Extract from SAMMS
        log(f"Extracting from {from_tbl_vw} WHERE year={year} …")
        src_df = extract_source_orders(src_constr, from_tbl_vw, where_cond, year)
        if src_df.empty:
            log("No source rows — skipping.")
            return True
        log(f"Extracted {len(src_df):,} source rows.")

        # 2. Build staging tuples
        staging_rows = _build_staging_rows(site_code, src_df)

        # 3. Stage + MERGE on destination
        with pyodbc.connect(dest_constr, autocommit=False) as conn:
            conn.fast_executemany = True
            cur = conn.cursor()

            cur.execute(_CREATE_STAGING_SQL)

            total = len(staging_rows)
            for start in range(0, total, batch_size):
                batch = staging_rows[start: start + batch_size]
                cur.executemany(_STAGING_INSERT_SQL, batch)
                log(f"  Staged {min(start + batch_size, total):,}/{total:,} rows …")

            cur.execute(_build_merge_sql(target_table, site_code))

            counts: dict[str, int] = {}
            for row in cur.fetchall():
                action = str(row[0]).strip().upper()
                counts[action] = counts.get(action, 0) + 1

            conn.commit()

        log(
            f"Done — "
            f"INSERT={counts.get('INSERT', 0):,}  "
            f"UPDATE={counts.get('UPDATE', 0):,}  "
            f"soft-delete={counts.get('DELETE', 0):,}"
        )
        return True

    except Exception as exc:
        print(f"{tag} ERROR: {exc}")
        if getattr(exc, "__cause__", None):
            print(f"  Caused by: {exc.__cause__}")
        return False


# =============================================================================
# SECTION 6 — NAMED YEAR ENTRY POINTS  (save_orders_2016 … save_orders_2028)
#              Equivalent of SaveOrders2016() … SaveOrders2028() in the C# file
# =============================================================================

# Mapping: year → destination table name
_YEAR_TABLE: dict[int, str] = {
    2016: "pats.tbl_Orders2016",
    2017: "pats.tbl_Orders2017",
    2018: "pats.tbl_Orders2018",
    2019: "pats.tbl_Orders2019",
    2020: "pats.tbl_Orders2020",
    2021: "pats.tbl_Orders2021",
    2022: "pats.tbl_Orders2022",
    2023: "pats.tbl_Orders2023",
    2024: "pats.tbl_Orders2024",
    2025: "pats.tbl_Orders2025",
    2026: "pats.tbl_Orders2026",
    2027: "pats.tbl_Orders2027",
    2028: "pats.tbl_Orders2028",
}


def _year_fn(year: int):
    """Factory that returns a named per-year function."""
    def _fn(
        site_code: str,
        src_constr: str,
        dest_constr: str,
        from_tbl_vw: str = "dbo.tblOrder",
        where_cond: str = "1=1",
        **kwargs,
    ) -> bool:
        return run_save_orders_year(
            year         = year,
            target_table = _YEAR_TABLE[year],
            site_code    = site_code,
            src_constr   = src_constr,
            dest_constr  = dest_constr,
            from_tbl_vw  = from_tbl_vw,
            where_cond   = where_cond,
            **kwargs,
        )
    _fn.__name__ = f"save_orders_{year}"
    _fn.__doc__ = (
        f"Upsert orders for year {year} into pats.tbl_Orders{year}.\n"
        f"Equivalent of SaveOrders.cs -> SaveOrders{year}().\n\n"
        "Parameters: site_code, src_constr, dest_constr, "
        "from_tbl_vw='dbo.tblOrder', where_cond='1=1'"
    )
    return _fn


# Create one named function per year and expose them as module-level names
save_orders_2016 = _year_fn(2016)
save_orders_2017 = _year_fn(2017)
save_orders_2018 = _year_fn(2018)
save_orders_2019 = _year_fn(2019)
save_orders_2020 = _year_fn(2020)
save_orders_2021 = _year_fn(2021)
save_orders_2022 = _year_fn(2022)
save_orders_2023 = _year_fn(2023)
save_orders_2024 = _year_fn(2024)
save_orders_2025 = _year_fn(2025)
save_orders_2026 = _year_fn(2026)
save_orders_2027 = _year_fn(2027)
save_orders_2028 = _year_fn(2028)


# =============================================================================
# SECTION 7 — ORCHESTRATOR
#              Equivalent of the year-split loop in BHGTaskRunner/Program.cs arg=11
# =============================================================================

def run_all_years_for_site(
    site_code: str,
    dest_constr: str,
    src_constr: str | None = None,
    from_tbl_vw: str = "dbo.tblOrder",
    where_cond: str = "1=1",
    verbose: bool = True,
) -> bool:
    """
    Run save_orders_2016 … save_orders_2028 in order for one clinic.

    Mirrors the failure gate in BHGTaskRunner: if any year returns False,
    all subsequent years are skipped and the overall result is False.

    If src_constr is None, it is read from ctrl.tbl_LocationCons in BHG_DR.
    If where_cond is '1=1', the caller should supply the actual WhereCondition
    from the child task row (tsk.tbl_Tasks2.WhereCondition).
    """
    start = datetime.now()
    print(f"\n{'='*62}")
    print(f"SaveOrders Orchestrator | site={site_code} | {start:%Y-%m-%d %H:%M:%S}")
    print(f"{'='*62}")

    if src_constr is None:
        src_constr = _load_site_constr(dest_constr, site_code)

    gate = True
    results: dict[int, bool] = {}

    for year in sorted(_YEAR_TABLE):
        if not gate:
            print(f"  [SKIPPED] {year} — gate closed by previous failure.")
            results[year] = False
            continue

        ok = run_save_orders_year(
            year         = year,
            target_table = _YEAR_TABLE[year],
            site_code    = site_code,
            src_constr   = src_constr,
            dest_constr  = dest_constr,
            from_tbl_vw  = from_tbl_vw,
            where_cond   = where_cond,
            verbose      = verbose,
        )
        results[year] = ok
        if not ok:
            gate = False

    elapsed = (datetime.now() - start).total_seconds()
    passed  = sum(1 for v in results.values() if v)
    print(f"\n{'-'*62}")
    print(f"Summary | site={site_code} | {passed}/{len(_YEAR_TABLE)} years OK | {elapsed:.1f}s")
    for y, ok in results.items():
        print(f"  {y}  {'OK  ' if ok else 'FAIL'}")
    print(f"{'='*62}\n")
    return gate


def run_all_sites(dest_constr: str, verbose: bool = True) -> dict[str, bool]:
    """
    Run run_all_years_for_site() for every active clinic in the task queue.
    Returns {site_code: success_bool}.
    """
    sites = _load_active_sites(dest_constr)
    print(f"SaveOrders full run — {len(sites)} active site(s) found.")
    overall: dict[str, bool] = {}
    for sc in sites:
        overall[sc] = run_all_years_for_site(sc, dest_constr, verbose=verbose)
    failed = [s for s, ok in overall.items() if not ok]
    print(f"\nAll-sites complete. Failed: {failed or 'none'}")
    return overall


# ---------------------------------------------------------------------------
# Control-table helpers
# ---------------------------------------------------------------------------

def _load_site_constr(dest_constr: str, site_code: str) -> str:
    """Read SAMMS connection string from ctrl.tbl_LocationCons."""
    sql = "SELECT ConStr FROM ctrl.tbl_LocationCons WHERE SiteCode = ?"
    with pyodbc.connect(dest_constr) as conn:
        row = conn.execute(sql, site_code).fetchone()
    if row is None:
        raise ValueError(f"No ctrl.tbl_LocationCons entry for SiteCode='{site_code}'")
    return str(row[0])


def _load_active_sites(dest_constr: str) -> list[str]:
    """Return SiteCodes with active SAMMS-ETL-Orders child tasks (Status=17)."""
    sql = """
    SELECT DISTINCT SiteCode FROM tsk.tbl_Tasks2
    WHERE TaskName = 'pats.tbl_orders'
      AND Status   = 17
      AND SiteCode <> 'PHC'
    ORDER BY SiteCode
    """
    with pyodbc.connect(dest_constr) as conn:
        return [str(r[0]) for r in conn.execute(sql).fetchall()]


# =============================================================================
# SECTION 8 — QUICK-START  (run as __main__ or paste into a Fabric Notebook cell)
# =============================================================================

if __name__ == "__main__":
    # ── Set your connection strings ──────────────────────────────────────────
    # In a Fabric Notebook use notebookutils to pull from Key Vault:
    #   import notebookutils
    #   DEST_CONSTR = notebookutils.credentials.getSecret("<kv_url>", "bhg-dr-azure-constr")
    #   SRC_CONSTR  = notebookutils.credentials.getSecret("<kv_url>", "samms-B01-constr")

    DEST_CONSTR = "<Azure BHG_DR connection string>"
    SRC_CONSTR  = "<SAMMS connection string for clinic>"
    SITE_CODE   = "B01"
    FROM_TBL_VW = "dbo.tblOrder"    # from dms.vw_MapAction
    WHERE_COND  = "1=1"             # from tsk.tbl_Tasks2.WhereCondition

    # ── Choose what to run ───────────────────────────────────────────────────

    # A) Single year
    # result = save_orders_2024(SITE_CODE, SRC_CONSTR, DEST_CONSTR, FROM_TBL_VW, WHERE_COND)
    # print("Result:", result)

    # B) All 13 years for one site (with failure gate)
    result = run_all_years_for_site(
        site_code   = SITE_CODE,
        dest_constr = DEST_CONSTR,
        src_constr  = SRC_CONSTR,
        from_tbl_vw = FROM_TBL_VW,
        where_cond  = WHERE_COND,
    )
    print("Overall result:", result)

    # C) All sites, all years (reads connection strings from ctrl.tbl_LocationCons)
    # run_all_sites(DEST_CONSTR)

# P1 Finance — Known Parity Issues

Expected differences between **BHG_DR** and **Fabric Silver** during migration validation. Not treated as bronze/silver bugs unless noted.

## Azure surrogate keys (null in Fabric)

| Table | Column | BHG | Fabric | Why |
| --- | --- | --- | --- | --- |
| `pats.tbl_PayerClient` | `PCID` | e.g. `471735` | `null` | Azure **IDENTITY** PK. Assigned on first insert in BHG_DR via `SavePayerClient`. **Not in SAMMS** — bronze intentionally nulls it. Match on `(SiteCode, pyID, abs(pyCLTID))`. |
| `pats.tbl_ClientDemo1` | `PrimKey` | populated | `null` | Same pattern — Azure surrogate, not sourced from SAMMS. |

**RowChkSum can still match** — checksum is built from source columns only, not these keys.

**If BHG `PCID`/`PrimKey` values are required:** one-time seed from BHG_DR by business key; daily SAMMS ETL cannot reproduce historical identity values.

## Empty string vs null — expected cosmetic gap

Optional text fields may show `""` in BHG_DR and `null` in Fabric (SavePayerClient, Save3pElig, FHA, etc.) because legacy EF uses `DataRow.ToString()` (`NULL` → `""`). Fabric silver keeps SQL/source nulls as null.

**Not a functional parity failure** when business keys and `RowChkSum` match. Do not blanket-coerce all string nulls to `""` in silver — that over-corrects and turns legitimate nulls into empty strings.

## Legacy bug parity (fixed in silver)

| Table | Issue | Status |
| --- | --- | --- |
| `tbl_FinancialHardshipApplication` | `ExpirationDate` populated from source `FHAPatientSignatureDate` in legacy (`SavePAData.cs`) | Fixed — silver coalesce transform |
| `tbl_pbi3PayAuth` | Date columns nulled by old `parse_legacy_date()` | Fixed — common cell + optional site backfill |

## Other expected gaps

| Table | Gap | Notes |
| --- | --- | --- |
| `tbl_ClientDemo2` | `REMARKS` null in Fabric | Investigate source/bronze mapping |
| `tbl_ClientDemo1` | `SalesForceId` null | Likely Azure-only / non-SAMMS field |
| `tbl_vw3pBillSub` | BHG parity export returned null row | Verify BHG query before treating as Fabric defect |

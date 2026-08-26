"""Generate P1 Reference silver notebooks with PascalCase target columns."""

import ast
import re
from pathlib import Path

BASE_DIR = Path(__file__).parent
SOURCE_DOC = BASE_DIR / "pl_p1_reference.txt"
OUTPUT_DIR = BASE_DIR / "SilverNotebooks"

NOTEBOOK_BY_METHOD = {
    "SaveClinic": "nb_p1_reference_sl_save_clinic",
    "Save3pSetup": "nb_p1_reference_sl_save_3p_setup",
    "SaveCodes": "nb_p1_reference_sl_save_codes",
    "SaveServices": "nb_p1_reference_sl_save_services",
    "SavedropDownListItems": "nb_p1_reference_sl_save_dropdown_list_items",
    "SaveCustomAnswers": "nb_p1_reference_sl_save_custom_answers",
    "SaveCustomQuestions": "nb_p1_reference_sl_save_custom_questions",
    "SavePreAdmissionV6": "nb_p1_reference_sl_save_pre_admission_v6",
    "SavePreAdminReferrals": "nb_p1_reference_sl_save_preadmin_referrals",
}

NOTEBOOK_SECTION_MARKER = (
    "================================================================================\n"
    "P1 REFERENCE BRONZE TO SILVER NOTEBOOKS"
)

NOTEBOOK_FOOTER = """
Important TaskConfig parity:
SaveCustomAnswers silver/gold dq keys must include CaQID because the legacy C# lookup is SiteCode + CaID + CaQID + CaCLTID. Update Silver/Gold TaskConfig merge_keys/dq_keys to PascalCase when deploying.
Silver Cell 2 `TARGET_COLUMNS` use PascalCase (first letter uppercased).
Cell 1 pascalizes TaskConfig `dq_keys` at runtime until Silver/Gold TaskConfig rows are updated.
Also see `SilverNotebooks/` for combined notebook `.py` files and `generate_reference_silver_notebooks.py` to regenerate.
"""

COMMON_CELL_PATCHES = [
    (
        "def dq_keys_from_taskconfig(task_row):",
        """def to_pascal_column(name):
    if not name:
        return name
    if name[0].isupper():
        return name
    return name[0].upper() + name[1:]


def pascalize_keys(keys):
    return [to_pascal_column(key) for key in keys]


def dq_keys_from_taskconfig(task_row):""",
    ),
    (
        "    match_keys = dq_keys_from_taskconfig(task_row)",
        "    match_keys = pascalize_keys(dq_keys_from_taskconfig(task_row))",
    ),
    (
        "def align_to_target(src_df, silver_table, configured_target_columns=None, transforms=None, drop_columns=None):\n"
        "    transforms = transforms or {}\n"
        "    cols = target_columns_for(src_df, silver_table, configured_target_columns, transforms, drop_columns)\n"
        "    target_schema = {}\n"
        "    if table_exists(silver_table):\n"
        "        target_schema = {f.name: f.dataType for f in spark.table(silver_table).schema.fields}\n\n"
        "    exprs = []\n"
        "    for target_col in cols:\n"
        "        if target_col in transforms:\n"
        "            expr = transforms[target_col]\n"
        "        else:\n"
        "            source_col = actual_col(src_df, target_col, required=False)\n"
        "            expr = F.col(source_col) if source_col else F.lit(None).cast(\"string\")\n"
        "        if target_col in target_schema:\n"
        "            expr = expr.cast(target_schema[target_col])\n"
        "        exprs.append(expr.alias(target_col))\n"
        "    return src_df.select(*exprs)",
        """def transform_expr(transform, src_df):
    return transform(src_df) if callable(transform) else transform


def align_to_target(src_df, silver_table, configured_target_columns=None, transforms=None, drop_columns=None):
    transforms = transforms or {}
    cols = target_columns_for(src_df, silver_table, configured_target_columns, transforms, drop_columns)
    target_schema = {}
    if table_exists(silver_table):
        target_schema = {f.name: f.dataType for f in spark.table(silver_table).schema.fields}

    exprs = []
    for target_col in cols:
        if target_col in transforms:
            expr = transform_expr(transforms[target_col], src_df)
        else:
            source_col = actual_col(src_df, target_col, required=False)
            expr = F.col(source_col) if source_col else F.lit(None).cast("string")
        if target_col in target_schema:
            expr = expr.cast(target_schema[target_col])
        exprs.append(expr.alias(target_col))
    return src_df.select(*exprs)""",
    ),
    (
        "        for key in match_keys:\n"
        "            actual_col(silver_df, key, required=True)\n"
        "            silver_df = silver_df.where(F.col(key).isNotNull())\n"
        "        silver_df = silver_df.dropDuplicates(match_keys)",
        """        resolved_keys = []
        for key in match_keys:
            resolved = actual_col(silver_df, key, required=True)
            resolved_keys.append(resolved)
            silver_df = silver_df.where(F.col(resolved).isNotNull())
        silver_df = silver_df.dropDuplicates(resolved_keys)""",
    ),
    (
        "            silver_df, legacy_skipped = apply_legacy_services_insert_scope(silver_df, silver_table, match_keys)",
        "            silver_df, legacy_skipped = apply_legacy_services_insert_scope(silver_df, silver_table, resolved_keys)",
    ),
    (
        "        target_keys = spark.table(silver_table).select(*match_keys).dropDuplicates()\n"
        "        rows_inserted = silver_df.join(target_keys, match_keys, \"left_anti\").count()\n"
        "        rows_updated = silver_df.join(target_keys, match_keys, \"inner\").count()",
        """        target_keys = spark.table(silver_table).select(*[actual_col(spark.table(silver_table), key, required=True) for key in match_keys]).dropDuplicates()
        join_keys = [actual_col(silver_df, key, required=True) for key in match_keys]
        target_join_keys = [actual_col(spark.table(silver_table), key, required=True) for key in match_keys]
        rows_inserted = silver_df.join(target_keys, join_keys, "left_anti").count()
        rows_updated = silver_df.join(target_keys, join_keys, "inner").count()""",
    ),
]

TRANSFORM_SOURCE_REWRITES = [
    ('"RowCheckSum": F.col("RowChkSum")', '"RowCheckSum": lambda df: col_or_null(df, "RowChkSum")'),
    ('F.col("caCLTID")', 'col_or_null(df, "CaCLTID")'),
    ('F.col("cID")', 'col_or_null(df, "CID")'),
    ('F.col("IsDeleted").cast("int")', 'col_or_null(df, "IsDeleted").cast("int")'),
    ('F.col("AccountNotInList").cast("int")', 'col_or_null(df, "AccountNotInList").cast("int")'),
    ('F.col("ContactNotInList").cast("int")', 'col_or_null(df, "ContactNotInList").cast("int")'),
    ('F.col("IsDeleted").cast("string")', 'col_or_null(df, "IsDeleted").cast("string")'),
    ('F.col("SiteID")', 'col_or_null(df, "SiteID")'),
    ('F.col("blHasPreloader")', 'col_or_null(df, "BlHasPreloader")'),
    ('F.col("IndividualNPI")', 'col_or_null(df, "IndividualNPI")'),
]


def to_pascal_column(name):
    if not name:
        return name
    if name[0].isupper():
        return name
    return name[0].upper() + name[1:]


def extract_notebook_section(text):
    marker = "P1 REFERENCE BRONZE TO SILVER NOTEBOOKS"
    start = text.index(marker)
    return text[start:]


def extract_common_cell(section):
    match = re.search(r"COMMON CELL.*?```python\n(.*?)```", section, re.S)
    if not match:
        raise RuntimeError("Common cell not found in pl_p1_reference.txt")
    cell = match.group(1)
    if "def pascalize_keys(keys):" in cell:
        return cell
    for old, new in COMMON_CELL_PATCHES:
        if old not in cell:
            raise RuntimeError(f"Common cell patch anchor not found: {old[:80]!r}")
        cell = cell.replace(old, new, 1)
    return cell


def split_method_cells(section):
    pattern = re.compile(
        r"-{80}\n(\d+)\. Notebook: (nb_p1_reference_sl_[^\n]+)\n-{80}\n\n```python\n(.*?)```",
        re.S,
    )
    blocks = pattern.findall(section)
    if len(blocks) != 9:
        raise RuntimeError(f"Expected 9 method cells, found {len(blocks)}")
    return blocks


def pascalize_string_literals(code):
    def repl(match):
        value = ast.literal_eval(match.group(0))
        if not isinstance(value, str):
            return match.group(0)
        return repr(to_pascal_column(value))

    return re.sub(r'("(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\')', repl, code)


def pascalize_transforms(code):
    if "TRANSFORMS = {" not in code:
        return code

    start = code.index("TRANSFORMS = {")
    end = code.index("}", start) + 1
    block = code[start:end]

    for old, new in TRANSFORM_SOURCE_REWRITES:
        block = block.replace(old, new)

    block = re.sub(
        r'"([A-Za-z][^"]*)"\s*:\s*(F\.[^,\n]+)',
        lambda m: f'"{to_pascal_column(m.group(1))}": lambda df: {m.group(2)}',
        block,
    )
    block = re.sub(
        r'"([A-Za-z][^"]*)"\s*:\s*(col_or_null\(df, [^)]+\)(?:\.cast\("[^"]+"\))?)',
        r'"\1": lambda df: \2',
        block,
    )
    return code[:start] + block + code[end:]


def pascalize_cell2(code):
    out = code
    triple = re.search(
        r'TARGET_COLUMNS = \[c\.strip\(\) for c in """(.*?)"""\.strip\(\)\.splitlines\(\)\]',
        out,
        re.S,
    )
    if triple:
        lines = [to_pascal_column(line.strip()) for line in triple.group(1).splitlines() if line.strip()]
        new_block = "TARGET_COLUMNS = [\n    " + ",\n    ".join(repr(line) for line in lines) + ",\n]"
        out = out[: triple.start()] + new_block + out[triple.end() :]
    elif "TARGET_COLUMNS = [" in out:
        start = out.index("TARGET_COLUMNS = [")
        depth = 0
        end = start
        for i, ch in enumerate(out[start:], start):
            if ch == "[":
                depth += 1
            elif ch == "]":
                depth -= 1
                if depth == 0:
                    end = i + 1
                    break
        block = out[start:end]
        out = out[:start] + pascalize_string_literals(block) + out[end:]
    out = pascalize_transforms(out)
    return out


def write_outputs():
    section = extract_notebook_section(SOURCE_DOC.read_text(encoding="utf-8"))
    common_cell = extract_common_cell(section)
    method_blocks = split_method_cells(section)

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    (OUTPUT_DIR / "nb_p1_reference_sl_common_cell1.py").write_text(common_cell, encoding="utf-8")

    md_parts = [
        "# P1 Reference Silver Notebooks\n",
        "Generated from `pl_p1_reference.txt` with PascalCase silver target columns.\n",
        "Silver Cell 2 `TARGET_COLUMNS` use PascalCase (first letter uppercased).\n",
        "Cell 1 pascalizes TaskConfig `dq_keys` at runtime until Silver/Gold TaskConfig is updated.\n",
        "\n## Cell 1 - Common Reference Silver Runtime\n",
        "```python\n" + common_cell + "```\n",
    ]

    cell2_marker = "\n# Cell 2\n"
    notebook_header = (
        "# Cell 1: paste/import nb_p1_reference_sl_common_cell1.py\n"
        "# Cell 2: method-specific code below\n\n"
    )

    method_by_notebook = {v: k for k, v in NOTEBOOK_BY_METHOD.items()}

    for _, notebook_name, cell2 in method_blocks:
        method = method_by_notebook[notebook_name]
        pascal_cell2 = pascalize_cell2(cell2)
        notebook_path = OUTPUT_DIR / f"{notebook_name}.py"
        notebook_text = (
            f"# Notebook: {notebook_name}\n"
            f"# Method: {method}\n"
            + notebook_header
            + common_cell
            + cell2_marker
            + pascal_cell2
        )
        notebook_path.write_text(notebook_text, encoding="utf-8")
        md_parts.extend([
            f"\n## Notebook: `{notebook_name}`\n",
            f"Method: `{method}`\n\n",
            "```python\n" + pascal_cell2 + "```\n",
        ])

    (OUTPUT_DIR / "P1_Reference_Silver_Notebook_Cells.md").write_text("".join(md_parts), encoding="utf-8")

    write_pl_p1_reference_doc(common_cell, method_blocks, method_by_notebook)


def write_pl_p1_reference_doc(common_cell, method_blocks, method_by_notebook):
    source_text = SOURCE_DOC.read_text(encoding="utf-8")
    marker_index = source_text.index(NOTEBOOK_SECTION_MARKER)
    pipeline_text = source_text[:marker_index].rstrip() + "\n\n"

    parts = [
        "================================================================================\n",
        "P1 REFERENCE BRONZE TO SILVER NOTEBOOKS\n",
        "Generated: 2026-06-26 (PascalCase silver columns refreshed via generate_reference_silver_notebooks.py)\n",
        "================================================================================\n\n",
        "Recommended notebook names:\n",
    ]
    for idx, notebook_name in enumerate(NOTEBOOK_BY_METHOD.values(), start=1):
        parts.append(f"{idx}. {notebook_name}\n")

    parts.extend([
        "\nUse the common helper cell below as Cell 1 in every notebook. Then paste the table-specific Cell 2 for that notebook.\n",
        "The notebooks return one JSON object keyed by the C# method name so the silver child pipeline can collect each table result independently.\n",
        "Combined Cell 1 + Cell 2 files live under `SilverNotebooks/`.\n\n",
        "--------------------------------------------------------------------------------\n",
        "COMMON CELL - paste as Cell 1 in all 9 notebooks\n",
        "--------------------------------------------------------------------------------\n\n",
        "```python\n",
        common_cell,
        "```\n",
    ])

    for idx, (_, notebook_name, cell2) in enumerate(method_blocks, start=1):
        method = method_by_notebook[notebook_name]
        pascal_cell2 = pascalize_cell2(cell2)
        parts.extend([
            f"\n--------------------------------------------------------------------------------\n",
            f"{idx}. Notebook: {notebook_name}\n",
            f"Method: {method}\n",
            "--------------------------------------------------------------------------------\n\n",
            "```python\n",
            pascal_cell2,
            "```\n",
        ])

    parts.append(NOTEBOOK_FOOTER)
    SOURCE_DOC.write_text(pipeline_text + "".join(parts), encoding="utf-8")


if __name__ == "__main__":
    write_outputs()
    print(f"Wrote common cell + 9 notebooks to {OUTPUT_DIR}")

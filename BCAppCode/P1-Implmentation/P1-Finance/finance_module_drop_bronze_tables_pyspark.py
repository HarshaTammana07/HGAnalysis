# Drop all 14 P1 Finance bronze tables for PascalCase reload.
# Run in a Fabric PySpark notebook attached to the Bronze lakehouse (bhg_bronze).

finance_bronze_tables = [
    "P1Finance.br_samms_bills",
    "P1Finance.br_samms_pbi3_pay_auth",
    "P1Finance.br_samms_vw3p_bill_sub",
    "P1Finance.br_samms_fmp",
    "P1Finance.br_samms_payer_clt_history",
    "P1Finance.br_samms_financial_hardship_application",
    "P1Finance.br_samms_3p_elig",
    "P1Finance.br_samms_claim_line_item",
    "P1Finance.br_samms_claim_line_item_activity",
    "P1Finance.br_samms_claims",
    "P1Finance.br_samms_payer_client",
    "P1Finance.br_samms_tbldiag10",
    "P1Finance.br_samms_client_demo1",
    "P1Finance.br_samms_client_demo2",
]

print("--- BEFORE ---")
for table_name in finance_bronze_tables:
    try:
        print(f"{table_name}: {spark.table(table_name).count()} rows")
    except Exception:
        print(f"{table_name}: not found")

print("\n--- DROP ---")
for table_name in finance_bronze_tables:
    spark.sql(f"DROP TABLE IF EXISTS {table_name}")
    print(f"Dropped: {table_name}")

print("\n--- AFTER ---")
for table_name in finance_bronze_tables:
    try:
        spark.table(table_name)
        print(f"STILL EXISTS: {table_name}")
    except Exception:
        print(f"OK (gone): {table_name}")

print("\nDone. Run finance Bronze pipeline, then Silver pipeline.")

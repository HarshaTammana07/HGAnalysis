-- P1 Reference module ETLConfig entries
-- ConfigId 88 = Bronze, 89 = Silver, 90 = Gold

MERGE INTO bhg_bronze.meta.etlconfig AS target
USING (
    SELECT
        88 AS ConfigId,
        'SAMMS P1 Reference Bronze Pipeline' AS ConfigName,
        'pl_reference' AS PipelineName,
        '/pipelines/pl_reference' AS PipelinePath,
        NULL AS TransformationModule,
        'SAMMS' AS SourceSystem,
        'DATA BASE' AS SourceType,
        'BR' AS TargetName,
        'DEV' AS EnvironmentName,
        NULL AS PipelineDependency,
        1 AS ExecutionSequence,
        'SCHEDULE' AS TriggerType,
        'DAILY' AS TriggerFrequency,
        'Fabric' AS TriggeredBy,
        1 AS IsActive,
        '{}' AS ConnectionConfig,
        'Harsha' AS CreatedBy,
        'Harsha' AS ModifiedBy

    UNION ALL

    SELECT
        89 AS ConfigId,
        'SAMMS P1 Reference Silver Pipeline' AS ConfigName,
        'nb_p1_reference_bronze_to_silver' AS PipelineName,
        '/notebooks/nb_p1_reference_bronze_to_silver' AS PipelinePath,
        '/notebooks/nb_p1_reference_bronze_to_silver' AS TransformationModule,
        'SAMMS' AS SourceSystem,
        'TABLE' AS SourceType,
        'SL' AS TargetName,
        'DEV' AS EnvironmentName,
        'P1_REFERENCE_BRONZE' AS PipelineDependency,
        2 AS ExecutionSequence,
        'SCHEDULE' AS TriggerType,
        'DAILY' AS TriggerFrequency,
        'Fabric' AS TriggeredBy,
        1 AS IsActive,
        '{}' AS ConnectionConfig,
        'Harsha' AS CreatedBy,
        'Harsha' AS ModifiedBy

    UNION ALL

    SELECT
        90 AS ConfigId,
        'SAMMS P1 Reference Gold Pipeline' AS ConfigName,
        'Execute_P1_Reference' AS PipelineName,
        '/pipelines/Execute_P1_Reference' AS PipelinePath,
        'Versioned Gold Publish' AS TransformationModule,
        'SAMMS' AS SourceSystem,
        'TABLE' AS SourceType,
        'GL' AS TargetName,
        'DEV' AS EnvironmentName,
        'P1_REFERENCE_SILVER' AS PipelineDependency,
        3 AS ExecutionSequence,
        'SCHEDULE' AS TriggerType,
        'DAILY' AS TriggerFrequency,
        'Fabric' AS TriggeredBy,
        1 AS IsActive,
        '{}' AS ConnectionConfig,
        'Harsha' AS CreatedBy,
        'Harsha' AS ModifiedBy
) AS source
ON target.ConfigId = source.ConfigId
WHEN MATCHED THEN
    UPDATE SET
        target.ConfigName = source.ConfigName,
        target.PipelineName = source.PipelineName,
        target.PipelinePath = source.PipelinePath,
        target.TransformationModule = source.TransformationModule,
        target.SourceSystem = source.SourceSystem,
        target.SourceType = source.SourceType,
        target.TargetName = source.TargetName,
        target.EnvironmentName = source.EnvironmentName,
        target.PipelineDependency = source.PipelineDependency,
        target.ExecutionSequence = source.ExecutionSequence,
        target.TriggerType = source.TriggerType,
        target.TriggerFrequency = source.TriggerFrequency,
        target.TriggeredBy = source.TriggeredBy,
        target.IsActive = source.IsActive,
        target.ConnectionConfig = source.ConnectionConfig,
        target.ModifiedBy = source.ModifiedBy,
        target.ModifiedAt = current_timestamp()
WHEN NOT MATCHED THEN
    INSERT (
        ConfigId,
        ConfigName,
        PipelineName,
        PipelinePath,
        TransformationModule,
        SourceSystem,
        SourceType,
        TargetName,
        EnvironmentName,
        PipelineDependency,
        ExecutionSequence,
        TriggerType,
        TriggerFrequency,
        TriggeredBy,
        IsActive,
        ConnectionConfig,
        CreatedBy,
        ModifiedBy,
        CreatedAt,
        ModifiedAt
    )
    VALUES (
        source.ConfigId,
        source.ConfigName,
        source.PipelineName,
        source.PipelinePath,
        source.TransformationModule,
        source.SourceSystem,
        source.SourceType,
        source.TargetName,
        source.EnvironmentName,
        source.PipelineDependency,
        source.ExecutionSequence,
        source.TriggerType,
        source.TriggerFrequency,
        source.TriggeredBy,
        source.IsActive,
        source.ConnectionConfig,
        source.CreatedBy,
        source.ModifiedBy,
        current_timestamp(),
        current_timestamp()
    );

SELECT
    ConfigId,
    ConfigName,
    PipelineName,
    PipelinePath,
    TransformationModule,
    SourceSystem,
    SourceType,
    TargetName,
    EnvironmentName,
    PipelineDependency,
    ExecutionSequence,
    TriggerType,
    TriggerFrequency,
    TriggeredBy,
    IsActive,
    ConnectionConfig,
    CreatedBy,
    ModifiedBy,
    CreatedAt,
    ModifiedAt
FROM bhg_bronze.meta.etlconfig
WHERE ConfigId IN (88, 89, 90)
ORDER BY ConfigId;


from delta.tables import DeltaTable
from pyspark.sql import functions as F

taskconfig_table = "bhg_bronze.meta.taskconfig"

bronze_config_id = 88
start_task_config_id = 4652
end_task_config_id = 5713
test_site_code = "AHK"

taskconfig = DeltaTable.forName(spark, taskconfig_table)

# 1. Make all P1 Reference Bronze site rows inactive.
# Leaves the generic template rows, Silver, and Gold untouched.
taskconfig.update(
    condition=f"""
        ConfigId = {bronze_config_id}
        AND TaskConfigId BETWEEN {start_task_config_id} AND {end_task_config_id}
        AND SiteCode IS NOT NULL
    """,
    set={"IsActive": F.lit(0)}
)

# 2. Activate only AHK for P1 Reference Bronze.
# This should activate 9 rows, one per Reference table/method.
taskconfig.update(
    condition=f"""
        ConfigId = {bronze_config_id}
        AND TaskConfigId BETWEEN {start_task_config_id} AND {end_task_config_id}
        AND upper(SiteCode) = '{test_site_code}'
    """,
    set={"IsActive": F.lit(1)}
)

# 3. Verify active Bronze rows.
display(spark.sql(f"""
SELECT
    TaskConfigId,
    ConfigId,
    TaskName,
    Method,
    SourceTable,
    TargetTable,
    SiteCode,
    DataBaseName,
    IsActive
FROM {taskconfig_table}
WHERE ConfigId = {bronze_config_id}
  AND TaskConfigId BETWEEN {start_task_config_id} AND {end_task_config_id}
  AND IsActive = 1
ORDER BY TaskConfigId
"""))

# 4. Summary check.
display(spark.sql(f"""
SELECT
    SiteCode,
    COUNT(*) AS ActiveRows
FROM {taskconfig_table}
WHERE ConfigId = {bronze_config_id}
  AND TaskConfigId BETWEEN {start_task_config_id} AND {end_task_config_id}
  AND IsActive = 1
GROUP BY SiteCode
ORDER BY SiteCode
"""))
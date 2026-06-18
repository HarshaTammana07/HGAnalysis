using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;

namespace BHG_DR_LIB.Models
{
    public partial class BHG_DRContext : DbContext
    {
        public BHG_DRContext()
        {
            this.Database.SetCommandTimeout(999);
        }
        public BHG_DRContext(DbContextOptions<BHG_DRContext> options)
            : base(options)
        {
        }
        public virtual DbSet<TblNewPeriodicReassessmentD2> TblNewPeriodicReassessmentD2s { get; set; }
        public virtual DbSet<TblNewPeriodicReassessmentD3> TblNewPeriodicReassessmentD3s { get; set; }
        public virtual DbSet<TblNewPeriodicReassessmentD4> TblNewPeriodicReassessmentD4s { get; set; }
        public virtual DbSet<TblNewPeriodicReassessmentD5> TblNewPeriodicReassessmentD5s { get; set; }
        public virtual DbSet<TblNewPeriodicReassessmentD6> TblNewPeriodicReassessmentD6s { get; set; }
        public virtual DbSet<TblNewPeriodicReassessment> TblNewPeriodicReassessments { get; set; }
        public virtual DbSet<TblNewPeriodicReassessmentCounselorReview> TblNewPeriodicReassessmentCounselorReviews { get; set; }
        public virtual DbSet<TblDiag10> TblDiag10s { get; set; }
        public virtual DbSet<TblBamForm> TblBamForms { get; set; }
        public virtual DbSet<TblBamScore> TblBamScores { get; set; }
        public virtual DbSet<TblMNComprehensiveAssessmentLevelOfCare> TblMNComprehensiveAssessmentLevelOfCares { get; set; }
        public virtual DbSet<TblMNComprehensiveAssessment> TblMNComprehensiveAssessments { get; set; }
        public virtual DbSet<TblFinancialHardshipApplication> TblFinancialHardshipApplications { get; set; }
        public virtual DbSet<TblPA> TblPAs { get; set; }
        public virtual DbSet<TblPACounselorReview> TblPACounselorReviews { get; set; }
        public virtual DbSet<TblPADimension1> TblPADimension1s { get; set; }
        public virtual DbSet<TblPADimension2> TblPADimension2s { get; set; }
        public virtual DbSet<TblPADimension3> TblPADimension3s { get; set; }
        public virtual DbSet<TblPADimension4> TblPADimension4s { get; set; }
        public virtual DbSet<TblPADimension5> TblPADimension5s { get; set; }
        public virtual DbSet<TblPADimension6> TblPADimension6s { get; set; }
        public virtual DbSet<TblAdmissionAssessmentDimensionFour> TblAdmissionAssessmentDimensionFour { get; set; }
        public virtual DbSet<TblAdmissionAssessmentSummary> TblAdmissionAssessmentSummary { get; set; }
        public virtual DbSet<TblAppointmentAttend> TblAppointmentAttends { get; set; }
        public virtual DbSet<Tbl3pArnote> Tbl3pArnote { get; set; }
        public virtual DbSet<Tbl3pClaimNote> Tbl3pClaimNote { get; set; }
        public virtual DbSet<TblAppointments> TblAppointments { get; set; }
        public virtual DbSet<Tbl3psetup> Tbl3psetup { get; set; }
        public virtual DbSet<TblEandMformMdm> TblEandMformMdm { get; set; }
        public virtual DbSet<TblEandMformPregnancy> TblEandMformPregnancy { get; set; }
        public virtual DbSet<TblVw3pbill> TblVw3pbill { get; set; }
        public virtual DbSet<TblPreAdmissionV6> TblPreAdmissionV6 { get; set; }
        public virtual DbSet<TblCols> TblCols { get; set; }
        public virtual DbSet<Tbl3pElig> Tbl3pElig { get; set; }
        public virtual DbSet<TblCowxref> TblCowxref { get; set; }
        public virtual DbSet<TblClinicalOpiateWithdrawalScale> TblClinicalOpiateWithdrawalScale { get; set; }
        public virtual DbSet<TblBriefAddictionMonitor> TblBriefAddictionMonitor { get; set; }
        public virtual DbSet<TblConsents_Phc> TblConsentsPhc { get; set; }
        public virtual DbSet<TblConsents> TblConsents { get; set; }
        public virtual DbSet<TblCowsV6> TblCowsV6 { get; set; }
        public virtual DbSet<TblDoseExcuse> TblDoseExcuse { get; set; }
        public virtual DbSet<TblDose> TblDose { get; set; }
        public virtual DbSet<YearlyAuditData> YearlyAuditData { get; set; }
        public virtual DbSet<TblPbi3Payauth> Pbi3Payauths { get; set; }
        public virtual DbSet<TblBills> TblBills { get; set; }
        public virtual DbSet<TblCheckIn> TblCheckIn { get; set; }
        public virtual DbSet<TblClaimLineItem> TblClaimLineItem { get; set; }
        public virtual DbSet<TblClaimLineItemActivity> TblClaimLineItemActivity { get; set; }
        public virtual DbSet<TblClaims> TblClaims { get; set; }
        public virtual DbSet<TblClientDemo1> TblClientDemo1 { get; set; }
        public virtual DbSet<TblClientDemo2> TblClientDemo2 { get; set; }
        public virtual DbSet<TblClinic> TblClinic { get; set; }
        public virtual DbSet<TblCodes> TblCodes { get; set; }
        public virtual DbSet<TblConnections> TblConnections { get; set; }
        public virtual DbSet<TblCtrlSiteTableInit> TblCtrlSiteTableInit { get; set; }
        public virtual DbSet<TblCtrlSiteTableInitLog> TblCtrlSiteTableInitLog { get; set; }
        public virtual DbSet<TblDartsSrv> TblDartsSrv { get; set; }
        public virtual DbSet<TblDartsSrv_2015> TblDartsSrv2015 { get; set; }
        public virtual DbSet<TblDartsSrv_2016> TblDartsSrv2016 { get; set; }
        public virtual DbSet<TblDartsSrv_2017> TblDartsSrv2017 { get; set; }
        public virtual DbSet<TblDartsSrv_2018> TblDartsSrv2018 { get; set; }
        public virtual DbSet<TblDartsSrv_2019> TblDartsSrv2019 { get; set; }
        public virtual DbSet<TblDartsSrv_2020> TblDartsSrv2020 { get; set; }
        public virtual DbSet<TblDartsSrv_2021> TblDartsSrv2021 { get; set; }
        public virtual DbSet<TblDartsSrv_2022> TblDartsSrv2022 { get; set; }
        public virtual DbSet<TblDartsSrv_2023> TblDartsSrv2023 { get; set; }
        public virtual DbSet<TblDartsSrv_2024> TblDartsSrv2024 { get; set; }
        public virtual DbSet<TblEnrollment> TblEnrollment { get; set; }
        public virtual DbSet<TblFormsSammsclient> TblFormsSammsclient { get; set; }
        public virtual DbSet<TblForms2Process> TblForms2Process { get; set; }
        public virtual DbSet<TblFeeSched> TblFeeSched { get; set; }
        public virtual DbSet<TblFmp> TblFmp { get; set; }
        public virtual DbSet<TblGlobalPayor> TblGlobalPayor { get; set; }
        public virtual DbSet<TblGlobalDevices> TblGlobalDevices { get; set; }
        public virtual DbSet<TblLocationCmds> TblLocationCmds { get; set; }
        public virtual DbSet<TblLocationCons> TblLocationCons { get; set; }
        public virtual DbSet<TblLocations> TblLocations { get; set; }
        public virtual DbSet<TblMapSrc2Dsn> TblMapSrc2Dsn { get; set; }
        public virtual DbSet<VwOrders> VwOrders { get; set; }
        public virtual DbSet<TblOrders> TblOrders { get; set; }
        public virtual DbSet<TblOrders2028> TblOrders2028 { get; set; }
        public virtual DbSet<TblOrders2027> TblOrders2027 { get; set; }
        public virtual DbSet<TblOrders2026> TblOrders2026 { get; set; }
        public virtual DbSet<TblOrders2025> TblOrders2025 { get; set; }
        public virtual DbSet<TblOrders2024> TblOrders2024 { get; set; }
        public virtual DbSet<TblOrders2023> TblOrders2023 { get; set; }
        public virtual DbSet<TblOrders2022> TblOrders2022 { get; set; }
        public virtual DbSet<TblOrders2021> TblOrders2021 { get; set; }
        public virtual DbSet<TblOrders2020> TblOrders2020 { get; set; }
        public virtual DbSet<TblOrders2019> TblOrders2019 { get; set; }
        public virtual DbSet<TblOrders2018> TblOrders2018 { get; set; }
        public virtual DbSet<TblOrders2017> TblOrders2017 { get; set; }
        public virtual DbSet<TblOrders2016> TblOrders2016 { get; set; }
        public virtual DbSet<TblPayerClient> TblPayerClient { get; set; }
        public virtual DbSet<TblSchedule> TblSchedule { get; set; }
        public virtual DbSet<TblServices> TblServices { get; set; }
        public virtual DbSet<TblTasks> TblTasks { get; set; }
        public virtual DbSet<TblTriggers> TblTriggers { get; set; }
        public virtual DbSet<TblUaresultDetail> TblUaresultDetail { get; set; }
        public virtual DbSet<TblUaresults> TblUaresults { get; set; }
        public virtual DbSet<TblUasched> TblUasched { get; set; }
        public virtual DbSet<TblUser> TblUser { get; set; }
        public virtual DbSet<TblUserSites> TblUserSites { get; set; }
        public virtual DbSet<TblXref> TblXref { get; set; }
        public virtual DbSet<VwMapAction> vwSubTask { get; set; }
        public virtual DbSet<VwMapSrc2Dsn> WorkToDo { get; set; }
        public virtual DbSet<VwReinitRunSchd> vwReInitTask { get; set; }
        public virtual DbSet<VwXref> VwXref { get; set; }
        public virtual DbSet<TblFormsCounts> TblFormsCounts { get; set; }
        public virtual DbSet<VwTaskListMap> VwTaskList { get; set; }
        public virtual DbSet<TblCustomAnswers> TblCustomAnswers { get; set; }
        public virtual DbSet<TblCustomQuestions> TblCustomQuestions { get; set; }
        public virtual DbSet<TblDboFormQuestionAnswers> TblDboFormQuestionAnswers { get; set; }
        public virtual DbSet<TblDboFormAnswerSignatures> TblDboFormAnswerSignatures { get; set; }
        public virtual DbSet<TblDboAnswerSignatures> TblDboAnswerSignatures { get; set; }
        public virtual DbSet<TblRowTrax> TblRowTrax { get; set; }
        public virtual DbSet<Tblvw3pBillSub> Tblvw3pBillSub { get; set; }
        public virtual DbSet<TblPayerCltHistory> TblPayerCltHistory { get; set; }
        public virtual DbSet<TblPreAdmissionReferralSource> TblPreAdmissionReferralSource { get; set; }
        public virtual DbSet<TblBottle> TblBottle { get; set; }
        public virtual DbSet<TblInvtype> TblInvtype { get; set; }
        public virtual DbSet<TblLiquidLog> TblLiquidLog { get; set; }
        public virtual DbSet<TblOrientationChecklistNew> TblOrientationChecklistNew { get; set; }
        public virtual DbSet<TblLabresult> TblLabresult { get; set; }
        public virtual DbSet<TblLabresultdetail> TblLabresultdetail { get; set; }
        public virtual DbSet<TblAdmissionAssessment> TblAdmissionAssessment { get; set; }
        public virtual DbSet<TblAdmissionAssessmentDimensionFiveSubstanceUse> TblAdmissionAssessmentDimensionFiveSubstanceUse { get; set; }
        public virtual DbSet<TblAdmissionAssessmentDimensionOneDisorder> TblAdmissionAssessmentDimensionOneDisorder { get; set; }
        public virtual DbSet<TblAdmissionAssessmentDimensionSix> TblAdmissionAssessmentDimensionSix { get; set; }
        public virtual DbSet<TblAdmissionAssessmentDimensionTwo> TblAdmissionAssessmentDimensionTwo { get; set; }
        public virtual DbSet<TblReAssessment> TblReAssessment { get; set; }
        public virtual DbSet<TblReAssessmentFamily> TblReAssessmentFamily { get; set; }
        public virtual DbSet<TblReAssessmentLegal> TblReAssessmentLegal { get; set; }
        public virtual DbSet<TblReAssessmentMentalHealth> TblReAssessmentMentalHealth { get; set; }
        public virtual DbSet<TblReAssessmentOccupational> TblReAssessmentOccupational { get; set; }
        public virtual DbSet<TblReAssessmentPhysicalHealth> TblReAssessmentPhysicalHealth { get; set; }
        public virtual DbSet<TblReAssessmentSocial> TblReAssessmentSocial { get; set; }
        public virtual DbSet<TblReAssessmentSubstanceUse> TblReAssessmentSubstanceUse { get; set; }
        public virtual DbSet<TblReAssessmentTreatment> TblReAssessmentTreatment { get; set; }
        public virtual DbSet<TblAdmissionAssessmentDimensionThree> TblAdmissionAssessmentDimensionThree { get; set; }
        public virtual DbSet<TblTreatmentLevel> TblTreatmentLevels { get; set; }
        public virtual DbSet<TblAssessmentSubstanceUseHistory> TblAssessmentSubstanceUseHistories { get; set; }
        public virtual DbSet<TblAdmissionAssessmentSubstanceUseHistory> TblAdmissionAssessmentSubstanceUseHistory { get; set; }
        public virtual DbSet<TblComprehensiveAssessmentForm> TblComprehensiveAssessmentForms { get; set; }
        public virtual DbSet<TblVAComprehensiveAssessment> TblVAComprehensiveAssessments { get; set; }
        public virtual DbSet<TblVAComprehensiveAssessmentSummary> TblVAComprehensiveAssessmentSummaries { get; set; }
        public virtual DbSet<TblNewAdmissionAssessment> TblNewAdmissionAssessments { get; set; }
        public virtual DbSet<TblNewAdmissionAssessmentASAMDimension2> TblNewAdmissionAssessmentASAMDimension2s { get; set; }
        public virtual DbSet<TblNewAdmissionAssessmentASAMDimension4> TblNewAdmissionAssessmentASAMDimension4s { get; set; }
        public virtual DbSet<TBLNewAdmissionAssessmentASAMDimension5> TBLNewAdmissionAssessmentASAMDimension5s { get; set; }
        public virtual DbSet<TblNewAdmissionAssessmentASAMDimension6> TblNewAdmissionAssessmentASAMDimension6s { get; set; }
        public virtual DbSet<TblClaimStatus> TblClaimStatuses { get; set; }
        public virtual DbSet<TblDropDownListItems> TblDropDownListItems {get; set;}
        public virtual DbSet<TblConsenttoMarketing> TblConsenttoMarketings { get; set; }
        public virtual DbSet<TblNewDischargeTransferPlanForm> TblNewDischargeTransferPlanForms { get; set; }
        public virtual DbSet<TblMNTreatmentServiceReview> TblMNTreatmentServiceReviews { get; set; }
        public virtual DbSet<TblTakeHomeAgreementandDiversionControl> TblTakeHomeAgreementandDiversionControls { get; set; }
        public virtual DbSet<TblDataForms> TblDataForms { get; set; }
        public virtual DbSet<TblTakeHomeRiskAssessment> TblTakeHomeRiskAssessments { get; set; }
        public virtual DbSet<TblSMSTextConsentForm> TblSMSTextConsentForms { get; set; }
        public virtual DbSet<TblSFPatientPreAdmission> TblSFPatientPreAdmissions { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseSqlServer("Data Source=bhgazuresql01.database.windows.net;Initial Catalog=BHG_DR;Persist Security Info=True;User ID=ayxbhg@bhgrecovery.onmicrosoft.com;Password=Alteryx#BHG2021;Authentication=\"Active Directory Password\"",
                    options => options.EnableRetryOnFailure()
                );

                optionsBuilder.EnableSensitiveDataLogging(true);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TblSFPatientPreAdmission>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.ID }).HasName("PK_SF_PATIENT_PRE_ADMISSION");
                entity.ToTable("tbl_SF_PatientPreAdmission", "pats");
            });

            modelBuilder.Entity<TblSMSTextConsentForm>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id }).HasName("PK_SMSTextConsetForm");
                entity.ToTable("tbl_SMSTextConsentForm", "pats");
            });

            modelBuilder.Entity<TblTakeHomeRiskAssessment>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id }).HasName("PK_TakeHomeRiskAssessment");
                entity.ToTable("tbl_TakeHomeRiskAssessment", "pats");
            });

            modelBuilder.Entity<TblDataForms>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id }).HasName("PK_SF_DataForms");
                entity.ToTable("tbl_SF_DataForms", "pats");
            });

            modelBuilder.Entity<TblNewDischargeTransferPlanForm>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                .HasName("PK_NewDischargeTransferPlanForm");
                entity.ToTable("tbl_NewDischargeTransferPlanForm", "pats");
            });
            modelBuilder.Entity<TblMNTreatmentServiceReview>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                .HasName("PK_MNTreatmentServiceReview");
                entity.ToTable("tbl_MNTreatmentServiceReview", "pats");
            });
            modelBuilder.Entity<TblTakeHomeAgreementandDiversionControl>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id }).HasName("PK_TakeHomeAgreementandDiversionControl");
                entity.ToTable("tbl_TakeHomeAgreementandDiversionControl", "pats");
            });
            modelBuilder.Entity<TblConsenttoMarketing>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id }).HasName("PK_ConsenttoMarketing");
                entity.ToTable("tbl_ConsenttoMarketing", "pats");
            });
            modelBuilder.Entity<TblNewPeriodicReassessmentD2>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.NewPeriodicReassessmentId })
                .HasName("PK_Tbl_NewPeriodicReassessmentD2");
            }
            );

            modelBuilder.Entity<TblNewPeriodicReassessmentD3>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.NewPeriodicReassessmentId }).HasName("PK_Tbl_NewPeriodicReassessmentD3");
            }
            );

            modelBuilder.Entity<TblNewPeriodicReassessmentD4>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.NewPeriodicReassessmentId }).HasName("PK_Tbl_NewPeriodicReassessmentD4");
            }
                        );
            modelBuilder.Entity<TblNewPeriodicReassessmentD5>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.NewPeriodicReassessmentId }).HasName("PK_Tbl_NewPeriodicReassessmentD5");
            }
                        );
            modelBuilder.Entity<TblNewPeriodicReassessmentD6>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.NewPeriodicReassessmentId }).HasName("PK_Tbl_NewPeriodicReassessmentD6");
            }
            );

            modelBuilder.Entity<TblNewPeriodicReassessment>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id }).HasName("PK_NewPeriodicReassessment");
            });

            modelBuilder.Entity<TblNewPeriodicReassessmentCounselorReview>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.NewPeriodicReassessmentId }).HasName("PK_NewPeriodicReassessmentCounselorReview");
            });

            modelBuilder.Entity<TblDiag10>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.dgID }).HasName("PK_tbldiag10");
            });

            modelBuilder.Entity<TblBamScore>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id }).HasName("PK_BAMScore");
            });

            modelBuilder.Entity<TblBamForm>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id }).HasName("PK_BamForm");
            });

            modelBuilder.Entity<TblClaimStatus>(entity =>
            {
                entity.HasKey(e => new { e.id }).HasName("PK_tblClaimStatus");
                entity.ToTable("tbl_claimstatus", "ctrl");
            }
                );
            modelBuilder.Entity<TblDropDownListItems>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id }).HasName("PK_DropDownListItems");
                entity.ToTable("tbl_DroDownListItems", "ctrl");
            }
                );

            modelBuilder.Entity<TblNewAdmissionAssessmentASAMDimension2>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.NewAdmissionAssessmentFormId }).HasName("PK_tbl_NewAdmissionAssessmentASAMDimension2");
            });

            modelBuilder.Entity<TblNewAdmissionAssessmentASAMDimension4>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.NewAdmissionAssessmentFormId }).HasName("PK_tbl_NewAdmissionAssessmentASAMDimension4");
            });

            modelBuilder.Entity<TBLNewAdmissionAssessmentASAMDimension5>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.NewAdmissionAssessmentFormId }).HasName("PK_tbl_NewAdmissionAssessmentASAMDimension5");
            });

            modelBuilder.Entity<TblNewAdmissionAssessmentASAMDimension6>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.NewAdmissionAssessmentFormId }).HasName("PK_NewAdmissionAssessmentASAMDimension6");
                entity.ToTable("tbl_NewAdmissionAssessmentASAMDimension6", "pats");
                entity.Property(e => e.SiteCode)
                .HasMaxLength(25)
                .IsUnicode(false);
            });

            modelBuilder.Entity<TblNewAdmissionAssessment>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id }).HasName("PK_NewAdmissionAssessment");
                entity.ToTable("tbl_NewAdmissionAssessment", "pats");
                entity.Property(e => e.SiteCode)
                .HasMaxLength(25)
                .IsUnicode(false);
            });
            modelBuilder.Entity<TblVAComprehensiveAssessmentSummary>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id }).HasName("PK_VAComprehensiveAssessmentSummary");
                entity.ToTable("tbl_VAComprehensiveAssessmentSummary", "pats");
                entity.Property(e => e.SiteCode)
                .HasMaxLength(25)
                .IsUnicode(false);
            });
            modelBuilder.Entity<TblVAComprehensiveAssessment>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id }).HasName("PK_VAComprehensiveAssessment");
                entity.ToTable("tbl_VAComprehensiveAssessment", "pats");
                entity.Property(e => e.SiteCode)
                .HasMaxLength(25)
                .IsUnicode(false);
            });

            modelBuilder.Entity<TblMNComprehensiveAssessmentLevelOfCare>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.MNComprehensiveAssessmentFormId }).HasName("PK_MNComprehensiveAssessmentLevelOfCare");
                entity.ToTable("tbl_MNComprehensiveAssessmentLevelOfCare", "pats");
                entity.Property(e => e.SiteCode)
                .HasMaxLength(25)
                .IsUnicode(false);
            });

            modelBuilder.Entity<TblMNComprehensiveAssessment>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id }).HasName("PK_MNComprehensiveAssessment");
                entity.ToTable("Tbl_MNComprehensiveAssessment", "pats");
                entity.Property(e => e.SiteCode)
                .HasMaxLength(25)
                .IsUnicode(false);
            });

            modelBuilder.Entity<TblAppointmentAttend>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.AAId }).HasName("PK_AppointmentAttend");
                entity.ToTable("Tbl_AppointmentAttend", "pats");
                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TblFinancialHardshipApplication>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id }).HasName("PK_FinancialHardshipApplication");
                entity.ToTable("Tbl_FinancialHardshipApplication", "pats");
                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TblPA>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id }).HasName("PK_tbl_PA");
                entity.ToTable("tbl_PA", "pats");
            });

            modelBuilder.Entity<TblPACounselorReview>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.PeriodicReassessmentId }).HasName("PK_PACounselorReview");
                entity.ToTable("Tbl_PACounselorReview", "pats");
                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TblPADimension1>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.PeriodicReassessmentId }).HasName("PK_PADimension1");
                entity.ToTable("Tbl_PADimension1", "pats");
                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TblPADimension2>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.PeriodicReassessmentId }).HasName("PK_PADimension2");
                entity.ToTable("Tbl_PADimension2", "pats");
                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TblPADimension3>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.PeriodicReassessmentId }).HasName("PK_PADimension3");
                entity.ToTable("Tbl_PADimension3", "pats");
                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TblPADimension4>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.PeriodicReassessmentId }).HasName("PK_PADimension4");
                entity.ToTable("Tbl_PADimension4", "pats");
                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TblPADimension5>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.PeriodicReassessmentId }).HasName("PK_PADimension5");
                entity.ToTable("Tbl_PADimension5", "pats");
                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TblPADimension6>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.PeriodicReassessmentId }).HasName("PK_PADimension6");
                entity.ToTable("Tbl_PADimension6", "pats");
                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TblComprehensiveAssessmentForm>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id }).HasName("PK_ComprehensiveAssessmentForm");
                entity.ToTable("Tbl_ComprehensiveAssessmentForm", "pats");
                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

            });

            modelBuilder.Entity<TblAssessmentSubstanceUseHistory>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id }).HasName("PK_AssessmentSubstanceUseHistory");
                entity.ToTable("Tbl_AssessmentSubstanceUseHistory", "pats");
                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

            });

            modelBuilder.Entity<TblAdmissionAssessmentSubstanceUseHistory>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id }).HasName("PK_AdmissionAssessmentSubstanceUseHistory");
                entity.ToTable("Tbl_AdmissionAssessmentSubstanceUseHistory", "pats");
                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

            });

            modelBuilder.Entity<TblTreatmentLevel>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.ID })
                .HasName("PK_tbl_TreatmentLevel");
                
                entity.ToTable("tbl_TreatmentLevel", "pats");
                
                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

            });

            modelBuilder.Entity<TblAdmissionAssessmentDimensionFour>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                    .HasName("PK_AdmissionAssessmentDimensionFour");

                entity.ToTable("tbl_AdmissionAssessmentDimensionFour", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.DdldimensionFourScore).HasColumnName("DDLDimensionFourScore");

                entity.Property(e => e.Dimension4Problems).IsUnicode(false);

                entity.Property(e => e.IdontThinkUseDrugsTooMuch).HasColumnName("IDontThinkUseDrugsTooMuch");

                entity.Property(e => e.IenjoyMyDrinking).HasColumnName("IEnjoyMyDrinking");

                entity.Property(e => e.IshouldCutDownOnMyDrinking).HasColumnName("IShouldCutDownOnMyDrinking");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.StageOfChange).HasMaxLength(100);
            });

            modelBuilder.Entity<TblAdmissionAssessmentSummary>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                    .HasName("PK_AdmissionAssessmentSummary");

                entity.ToTable("tbl_AdmissionAssessmentSummary", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.AdmissionAssessmentPatientSignatureBy).HasMaxLength(200);

                entity.Property(e => e.AdmissionAssessmentPatientSignatureDate).HasColumnType("datetime");

                entity.Property(e => e.AdmissionAssessmentProviderSignatureBy).HasMaxLength(200);

                entity.Property(e => e.AdmissionAssessmentProviderSignatureDate).HasColumnType("datetime");

                entity.Property(e => e.AdmissionAssessmentStaffSignatureBy).HasMaxLength(100);

                entity.Property(e => e.AdmissionAssessmentStaffSignatureDate).HasColumnType("datetime");

                entity.Property(e => e.AdmissionAssessmentSupervisorSignatureBy).HasMaxLength(200);

                entity.Property(e => e.AdmissionAssessmentSupervisorSignatureDate).HasColumnType("datetime");

                entity.Property(e => e.AsamrecommendationForLevel).HasColumnName("ASAMRecommendationForLevel");

                entity.Property(e => e.Ddlrecommendation).HasColumnName("DDLRecommendation");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");
            });

        modelBuilder.Entity<TblAdmissionAssessment>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                    .HasName("PK_AdmissionAssessment");

                entity.ToTable("tbl_AdmissionAssessment", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedBy).HasMaxLength(50);

                entity.Property(e => e.CreatedOn).HasColumnType("datetime");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.ModifiedBy).HasMaxLength(50);

                entity.Property(e => e.ModifiedOn).HasColumnType("datetime");

                entity.Property(e => e.Version).HasMaxLength(25);
            });

            modelBuilder.Entity<TblAdmissionAssessmentDimensionFiveSubstanceUse>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                    .HasName("PK_DimensionFiveSubstanceUse");

                entity.ToTable("tbl_AdmissionAssessmentDimensionFiveSubstanceUse", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.AnyOpenCasesWitHlocalDepartment).HasColumnName("AnyOpenCasesWitHLocalDepartment");

                entity.Property(e => e.Dimension5Problems).IsUnicode(false);

                entity.Property(e => e.LastModAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<TblAdmissionAssessmentDimensionThree>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                    .HasName("PK_AdmissionAssessmentDimensionThree");

                entity.ToTable("Tbl_AdmissionAssessmentDimensionThree", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.DdldimensionThreeScore).HasColumnName("DDLDimensionThreeScore");

                entity.Property(e => e.DidYouAttendSpecialEducation).HasColumnName("didYouAttendSpecialEducation");

                entity.Property(e => e.Dimension3Problems).IsUnicode(false);

                entity.Property(e => e.DoYouHaveApsychiatrist).HasColumnName("DoYouHaveAPsychiatrist");

                entity.Property(e => e.DoYouHaveApsychiatristTxt)
                    .HasMaxLength(100)
                    .HasColumnName("DoYouHaveAPsychiatristTxt");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<TblAdmissionAssessmentDimensionOneDisorder>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                    .HasName("PK_DimensionOneDisorder");

                entity.ToTable("tbl_AdmissionAssessmentDimensionOneDisorder", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.ChkPreviousMat).HasColumnName("ChkPreviousMAT");

                entity.Property(e => e.DdldimensionOneScore).HasColumnName("DDLDimensionOneScore");

                entity.Property(e => e.DdlhowLongDidYouTakeIt).HasColumnName("DDLHowLongDidYouTakeIt");

                entity.Property(e => e.DdlinpatientRehabilitation).HasColumnName("DDLInpatientRehabilitation");

                entity.Property(e => e.DdlintensiveOutpatient).HasColumnName("DDLIntensiveOutpatient");

                entity.Property(e => e.DdllongestPeriodOfSobrietyFromAllSubstances).HasColumnName("DDLLongestPeriodOfSobrietyFromAllSubstances");

                entity.Property(e => e.DdlmedicallyAssistedWithdrawal).HasColumnName("DDLMedicallyAssistedWithdrawal");

                entity.Property(e => e.DdloutpatientTreatment).HasColumnName("DDLOutpatientTreatment");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.PreviousMat).HasColumnName("PreviousMAT");

                entity.Property(e => e.PreviousMatbuprenorphine).HasColumnName("PreviousMATBuprenorphine");

                entity.Property(e => e.PreviousMatmethadone).HasColumnName("PreviousMATMethadone");

                entity.Property(e => e.PreviousMatnaltrexone).HasColumnName("PreviousMATNaltrexone");

                entity.Property(e => e.PreviousMatwasItHelpful).HasColumnName("PreviousMATWasItHelpful");

                entity.Property(e => e.PreviousMatwhatWasYourDose)
                    .HasMaxLength(50)
                    .HasColumnName("PreviousMATWhatWasYourDose");
            });

            modelBuilder.Entity<TblAdmissionAssessmentDimensionSix>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                    .HasName("PK_AdmissionAssessmentDimensionSix");

                entity.ToTable("tbl_AdmissionAssessmentDimensionSix", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.DdldimensionSixScore).HasColumnName("DDLDimensionSixScore");

                entity.Property(e => e.Dimension6Problems).IsUnicode(false);

                entity.Property(e => e.FamilyMembersWhoAreInRecovery).HasColumnName("familyMembersWhoAreInRecovery");

                entity.Property(e => e.FamilyWhoYouCanCountOnToSupport).HasColumnName("familyWhoYouCanCountOnToSupport");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<TblAdmissionAssessmentDimensionTwo>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                    .HasName("PK_AdmissionAssessmentDimensionTwo");

                entity.ToTable("tbl_AdmissionAssessmentDimensionTwo", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.Copdemphysema).HasColumnName("COPDemphysema");

                entity.Property(e => e.DdldimensionTwoScore).HasColumnName("DDLDimensionTwoScore");

                entity.Property(e => e.Dimension2Problems).IsUnicode(false);

                entity.Property(e => e.Gerd).HasColumnName("GERD");

                entity.Property(e => e.Hiv).HasColumnName("HIV");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<TblReAssessment>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                    .HasName("PK_ReAssessment");

                entity.ToTable("tbl_ReAssessment", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedBy).HasMaxLength(100);

                entity.Property(e => e.CreatedOn).HasColumnType("datetime");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.ModifiedBy).HasMaxLength(100);

                entity.Property(e => e.ModifiedOn).HasColumnType("datetime");

                entity.Property(e => e.Version).HasMaxLength(25);
            });

            modelBuilder.Entity<TblReAssessmentFamily>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                    .HasName("PK_ReAssessmentFamily");

                entity.ToTable("tbl_ReAssessmentFamily", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.LastModAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<TblReAssessmentLegal>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                    .HasName("PK_ReAssessmentLegal");

                entity.ToTable("tbl_ReAssessmentLegal", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.AreYouInvolvedWithAdrugTreatmentCourt).HasColumnName("AreYouInvolvedWithADrugTreatmentCourt");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<TblReAssessmentMentalHealth>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                    .HasName("PK_ReAssessmentMentalHealth");

                entity.ToTable("tbl_ReAssessmentMentalHealth", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.DoYouHaveApsychiatrist).HasColumnName("DoYouHaveAPsychiatrist");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<TblReAssessmentOccupational>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                    .HasName("PK_ReAssessmentOccupational");

                entity.ToTable("tbl_ReAssessmentOccupational", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.AreYouCurrentlyAfulltimeStudent).HasColumnName("AreYouCurrentlyAFulltimeStudent");

                entity.Property(e => e.AreYouCurrentlyAparttimeStudent).HasColumnName("AreYouCurrentlyAParttimeStudent");

                entity.Property(e => e.HaveYouFoundAparttimeOrFulltimeJob).HasColumnName("HaveYouFoundAParttimeOrFulltimeJob");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<TblReAssessmentPhysicalHealth>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                    .HasName("PK_ReAssessmentPhysicalHealth");

                entity.ToTable("tbl_ReAssessmentPhysicalHealth", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.ChkboxHepatitisCnegative).HasColumnName("ChkboxHepatitisCNegative");

                entity.Property(e => e.ChkboxHepatitisCpostive).HasColumnName("ChkboxHepatitisCPostive");

                entity.Property(e => e.ChkboxHivnegative).HasColumnName("ChkboxHIVNegative");

                entity.Property(e => e.ChkboxHivpostive).HasColumnName("ChkboxHIVPostive");

                entity.Property(e => e.ChkboxNa).HasColumnName("ChkboxNA");

                entity.Property(e => e.DoYouHaveAprimaryCarePractitionerOrClinic).HasColumnName("DoYouHaveAPrimaryCarePractitionerOrClinic");

                entity.Property(e => e.HaveYouBeenTestedForHivandHepatitisC).HasColumnName("HaveYouBeenTestedForHIVAndHepatitisC");

                entity.Property(e => e.HaveYouCalled911OrBeeniItheEmergencyRoom).HasColumnName("HaveYouCalled911OrBeeniITheEmergencyRoom");

                entity.Property(e => e.IfYouWereHepatitisCpositive).HasColumnName("IfYouWereHepatitisCPositive");

                entity.Property(e => e.IfYouWereHivpositive).HasColumnName("IfYouWereHIVPositive");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<TblReAssessmentSocial>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                    .HasName("PK_ReAssessmentSocial");

                entity.ToTable("tbl_ReAssessmentSocial", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.DoYouHaveAnyFriendsRorFamilyMembersWhoDontDrink).HasColumnName("DoYouHaveAnyFriendsROrFamilyMembersWhoDontDrink");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<TblReAssessmentSubstanceUse>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                    .HasName("PK_ReAssessmentSubstanceUse");

                entity.ToTable("tbl_ReAssessmentSubstanceUse", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.LastModAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<TblReAssessmentTreatment>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                    .HasName("PK_ReAssessmentTreatment");

                entity.ToTable("tbl_ReAssessmentTreatment", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.LastModAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<TblAppointments>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.UniqueId })
                    .HasName("PK_Appointments");

                entity.ToTable("tbl_Appointments", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.AppointmentType)
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.Area)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("area");

                entity.Property(e => e.CustomField1).IsUnicode(false);

                entity.Property(e => e.EndDate).HasColumnType("smalldatetime");

                entity.Property(e => e.IsSalesForceSync).HasColumnName("isSalesForceSync");

                entity.Property(e => e.IsThirdPartySync).HasColumnName("isThirdPartySync");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.SalesForceId)
                    .HasMaxLength(250)
                    .HasColumnName("salesForceId");

                entity.Property(e => e.Service)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("service");

                entity.Property(e => e.ServiceModifier)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.StartDate).HasColumnType("smalldatetime");

                entity.Property(e => e.TxtNote)
                    .HasMaxLength(500)
                    .IsUnicode(false)
                    .HasColumnName("txtNOTE");
            });

            modelBuilder.Entity<Tbl3pArnote>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.ArnId })
                    .HasName("PK_ARNotes");

                entity.ToTable("tbl_3pARNOTE", "pats");

                entity.Property(e => e.SiteCode).HasMaxLength(25);

                entity.Property(e => e.ArnId).HasColumnName("arnID");

                entity.Property(e => e.ArnDate)
                    .HasColumnType("datetime")
                    .HasColumnName("arnDATE");

                entity.Property(e => e.ArnDbnotes)
                    .HasMaxLength(250)
                    .IsUnicode(false)
                    .HasColumnName("arnDBnotes");

                entity.Property(e => e.ArnDtRemoved)
                    .HasColumnType("datetime")
                    .HasColumnName("arnDtRemoved");

                entity.Property(e => e.ArnLiid).HasColumnName("arnLIID");

                entity.Property(e => e.ArnNote)
                    .IsUnicode(false)
                    .HasColumnName("arnNOTE");

                entity.Property(e => e.ArnStrRemovedReason)
                    .IsUnicode(false)
                    .HasColumnName("arnStrRemovedReason");

                entity.Property(e => e.ArnStrRemovedUser)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("arnStrRemovedUser");

                entity.Property(e => e.ArnUser)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("arnUSER");

                entity.Property(e => e.Bid).HasColumnName("bid");

                entity.Property(e => e.GlobalBatchId).HasColumnName("globalBatchId");
            });

            modelBuilder.Entity<Tbl3pClaimNote>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Tpcn })
                    .HasName("PK_ClaimNotes");

                entity.ToTable("tbl_3pClaimNote", "pats");

                entity.Property(e => e.SiteCode).HasMaxLength(25);

                entity.Property(e => e.Tpcn).HasColumnName("tpcn");

                entity.Property(e => e.GlobalBatchId).HasColumnName("globalBatchId");

                entity.Property(e => e.TpcnDtTickler)
                    .HasColumnType("datetime")
                    .HasColumnName("tpcnDtTickler");

                entity.Property(e => e.TpcnDtTicklerRemoved)
                    .IsUnicode(false)
                    .HasColumnName("tpcnDtTicklerRemoved");

                entity.Property(e => e.TpcnDtmAdded)
                    .HasColumnType("datetime")
                    .HasColumnName("tpcnDtmAdded");

                entity.Property(e => e.TpcnStrAdded)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("tpcnStrAdded");

                entity.Property(e => e.TpcnStrNote)
                    .HasMaxLength(1000)
                    .IsUnicode(false)
                    .HasColumnName("tpcnStrNote");

                entity.Property(e => e.TpcnStrTicklerRemovedNote)
                    .IsUnicode(false)
                    .HasColumnName("tpcnStrTicklerRemovedNote");

                entity.Property(e => e.TpcnStrTicklerRemovedUser)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("tpcnStrTicklerRemovedUser");

                entity.Property(e => e.TpcnStrTicklerType)
                    .HasMaxLength(500)
                    .IsUnicode(false)
                    .HasColumnName("tpcnStrTicklerType");

                entity.Property(e => e.TpcnStrType)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("tpcnStrType");

                entity.Property(e => e.TpcnTpcid).HasColumnName("tpcnTPCID");
            });

            modelBuilder.Entity<Tbl3psetup>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e._pId })
                    .HasName("PK_tbl3PSETUP");

                entity.ToTable("tbl_3PSETUP", "ctrl");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e._pId).HasColumnName("pID");

                entity.Property(e => e.Address)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.BlHasPreloader).HasColumnName("blHasPreloader");

                entity.Property(e => e.City)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Clia)
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Clinic)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.Drfname)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("DRfname");

                entity.Property(e => e.Drlname)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("DRlname");

                entity.Property(e => e.Drnpi)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("DRnpi");

                entity.Property(e => e.IndividualNpi).HasColumnName("IndividualNPI");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.Medicaid)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Npi)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("NPI");

                entity.Property(e => e.ProviderAddress)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ProviderCity)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ProviderDesc)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.ProviderName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ProviderPhone)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ProviderState)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ProviderZip)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Sftppw)
                    .HasMaxLength(256)
                    .HasColumnName("SFTPPW");

                entity.Property(e => e.Sftpun)
                    .HasMaxLength(256)
                    .HasColumnName("SFTPUN");

                entity.Property(e => e.SiteId).HasColumnName("SiteID");

                entity.Property(e => e.State)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.StrDbnotes)
                    .HasMaxLength(250)
                    .IsUnicode(false)
                    .HasColumnName("strDBNotes");

                entity.Property(e => e.TaxId)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("TaxID");

                entity.Property(e => e.Taxonomy)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.Zip)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TblEandMformMdm>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                    .HasName("PK_EandMFormMDM");

                entity.ToTable("tbl_EandMFormMDM", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Context).HasMaxLength(128);

                entity.Property(e => e.CreatedBy).HasMaxLength(128);

                entity.Property(e => e.CreatedOn).HasColumnType("datetime");

                entity.Property(e => e.FormDate).HasColumnType("datetime");

                entity.Property(e => e.MedicalProviderSignatureBy)
                    .HasMaxLength(200)
                    .HasColumnName("MEdicalProviderSignatureBy");

                entity.Property(e => e.MedicalProviderSignatureDate)
                    .HasColumnType("datetime")
                    .HasColumnName("MEdicalProviderSignatureDate");

                entity.Property(e => e.ModifiedBy).HasMaxLength(128);

                entity.Property(e => e.ModifiedOn).HasColumnType("datetime");

                entity.Property(e => e.Version).HasMaxLength(128);
            });
            modelBuilder.Entity<TblEandMformPregnancy>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.EandMformId })
                    .HasName("PK_EandMFormPregnancy");

                entity.ToTable("tbl_EandMFormPregnancy", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.EandMformId).HasColumnName("EandMFormId");
                entity.Property(e => e.Context).HasMaxLength(128);

                entity.Property(e => e.CreatedBy).HasMaxLength(128);

                entity.Property(e => e.CreatedOn).HasColumnType("datetime");

                entity.Property(e => e.FormDate).HasColumnType("datetime");

                entity.Property(e => e.DateOfLastOb)
                    .HasColumnType("datetime")
                    .HasColumnName("DateOfLastOB");

                entity.Property(e => e.Ddltrimester).HasColumnName("DDLTrimester");

                entity.Property(e => e.DeliveriesTxt).HasMaxLength(50);

                entity.Property(e => e.DoseStabilityTxt).HasMaxLength(50);

                entity.Property(e => e.DoseTxt).HasMaxLength(50);

                entity.Property(e => e.IllicitDrugTxt).HasMaxLength(256);

                entity.Property(e => e.MgTxt).HasMaxLength(50);

                entity.Property(e => e.NameofObtxt)
                    .HasMaxLength(50)
                    .HasColumnName("NameofOBTxt");

                entity.Property(e => e.NapregnancyGrid).HasColumnName("NAPregnancyGrid");

                entity.Property(e => e.NoOfPregnanciesTxt).HasMaxLength(50);

                entity.Property(e => e.PregnancyOtherTxt).HasMaxLength(256);

                entity.Property(e => e.Provider).HasMaxLength(128);

                entity.Property(e => e.UdsradioBtn).HasColumnName("UDSRadioBtn");
                
                entity.Property(e => e.ModifiedBy).HasMaxLength(128);

                entity.Property(e => e.ModifiedOn).HasColumnType("datetime");

                entity.Property(e => e.Version).HasMaxLength(128);

                entity.Property(e => e.Wttxt)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("WTTxt");
            });

            modelBuilder.Entity<TblVw3pbill>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.DsId })
                    .HasName("PK_tbl_vw3pBill");

                entity.ToTable("tbl_vw3pbill", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.DsId).HasColumnName("dsID");

                entity.Property(e => e.BillUnits).HasColumnName("billUnits");

                entity.Property(e => e.Billdatecriteria)
                    .HasColumnName("billdatecriteria")
                    .HasColumnType("datetime");

                entity.Property(e => e.Charge).HasColumnName("charge");

                entity.Property(e => e.Clientname)
                    .HasColumnName("clientname")
                    .HasMaxLength(152)
                    .IsUnicode(false);

                entity.Property(e => e.CltAdd1)
                    .HasColumnName("cltADD1")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.CltCity)
                    .HasColumnName("cltCity")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.CltDob)
                    .HasColumnName("cltDOB")
                    .HasColumnType("datetime");

                entity.Property(e => e.CltGender)
                    .HasColumnName("cltGender")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.CltM4id)
                    .HasColumnName("cltM4ID")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.CltMarry)
                    .HasColumnName("cltMARRY")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.CltPhone)
                    .HasColumnName("cltPhone")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.CltState)
                    .HasColumnName("cltState")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Cltzip)
                    .HasColumnName("cltzip")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Cptcode)
                    .HasColumnName("CPTCODE")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Descript)
                    .IsRequired()
                    .HasColumnName("descript")
                    .HasMaxLength(17)
                    .IsUnicode(false);

                entity.Property(e => e.DsClt).HasColumnName("dsClt");

                entity.Property(e => e.DsDtEnd)
                    .HasColumnName("dsDtEnd")
                    .HasColumnType("datetime");

                entity.Property(e => e.DsDtStart)
                    .HasColumnName("dsDtStart")
                    .HasColumnType("datetime");

                entity.Property(e => e.DsPos)
                    .HasColumnName("dsPOS")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.DsTxtSrv)
                    .HasColumnName("dsTxtSrv")
                    .HasMaxLength(100);

                entity.Property(e => e.DsTxtType)
                    .HasColumnName("dsTxtType")
                    .HasMaxLength(50);

                entity.Property(e => e.Dsarea)
                    .HasColumnName("dsarea")
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.Dsbilled)
                    .HasColumnName("DSbilled")
                    .HasColumnType("datetime");

                entity.Property(e => e.DsdblUnits).HasColumnName("dsdblUnits");

                entity.Property(e => e.Dsdiag)
                    .HasColumnName("dsdiag")
                    .HasMaxLength(5)
                    .IsUnicode(false);

                entity.Property(e => e.DstxtStaff)
                    .HasColumnName("dstxtStaff")
                    .HasMaxLength(100);

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.Mg).HasColumnName("MG");

                entity.Property(e => e.Modifier)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Ndc)
                    .HasColumnName("NDC")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Npi)
                    .HasColumnName("npi")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.PayDefaultsubmit)
                    .HasColumnName("payDEFAULTSUBMIT")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Payclass)
                    .HasColumnName("payclass")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.PyGroup)
                    .HasColumnName("pyGROUP")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.PyPayerid)
                    .HasColumnName("pyPAYERID")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.PySubsid)
                    .HasColumnName("pySUBSID")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ScrubError)
                    .HasMaxLength(14)
                    .IsUnicode(false);

                entity.Property(e => e.SiteId).HasColumnName("SiteID");

                entity.Property(e => e.TpaAuthCode)
                    .HasColumnName("tpaAuthCode")
                    .HasMaxLength(100)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TblPreAdmissionV6>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.PreAdmissionid, e.Clientid })
                    .HasName("PK_PreAdmissions_V6");

                entity.ToTable("tbl_PreAdmission_V6", "ayx");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.AccomodationNeeded).HasMaxLength(256);

                entity.Property(e => e.AreYouCurrentlyPregnant)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.BringIdproof)
                    .HasColumnName("BringIDProof")
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.BringInsuranceCard)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.ClientAddress)
                    .HasMaxLength(2500)
                    .IsUnicode(false);

                entity.Property(e => e.ClinicInfo)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.Comments).HasMaxLength(500);

                entity.Property(e => e.CreatedOn)
                    .HasColumnName("CreatedON")
                    .HasColumnType("datetime");

                entity.Property(e => e.Createdby).HasMaxLength(40);

                entity.Property(e => e.CurrntlyRecevingTreatmentForCondition)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.DateofRelease).HasColumnType("datetime");

                entity.Property(e => e.EtllastModAt)
                    .HasColumnName("ETLLastModAt")
                    .HasColumnType("datetime");

                entity.Property(e => e.ImmediateAssessment).HasMaxLength(256);

                entity.Property(e => e.ImmediateAssessment911).HasMaxLength(256);

                entity.Property(e => e.InsuranceType).HasMaxLength(256);

                entity.Property(e => e.IntakeProgram).HasMaxLength(300);

                entity.Property(e => e.IntakeProgramDate).HasColumnType("datetime");

                entity.Property(e => e.IsAnyLegalPrescriptionForPain)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.IsAnyOngoingMedicalCondition)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.IsAnyPrescriptionForPain)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.IsCurrentlyInOpiateProgram)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.IsHavingLegalPrescription)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.IsHavingPlanForHowToCommitSuicide)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.IsHomicidalThoughtWithin72Hours)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.IsInsurance)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.IsOverTheCounterMedications)
                    .HasColumnName("isOverTheCounterMedications")
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.IsPatientAdmitted)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.IsPatientAtPainManagementClinic)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.IsRecentlyReleasedFromPenal)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.IsSpecialAccommodationRequired)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.IsSuicidalThoughtWithin72Hours)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdateOn).HasColumnType("datetime");

                entity.Property(e => e.LastUpdatedBy).HasMaxLength(40);

                entity.Property(e => e.MedicalConditionsProviderName1).HasMaxLength(256);

                entity.Property(e => e.MedicalConditionsProviderName2).HasMaxLength(256);

                entity.Property(e => e.MedicalConditionsProviderPhone1).HasMaxLength(50);

                entity.Property(e => e.MedicalConditionsProviderPhone2).HasMaxLength(50);

                entity.Property(e => e.OfficeUseWhy).HasMaxLength(50);

                entity.Property(e => e.OngoingMedicalConditionsWha).HasMaxLength(256);

                entity.Property(e => e.PatientSignatureDate).HasColumnType("datetime");

                entity.Property(e => e.PlanOfSuicide)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.PlanOnSpendingTimeAtClinic)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.PreAddAddress)
                    .HasColumnName("PreAdd_Address")
                    .HasMaxLength(500);

                entity.Property(e => e.PreAdmissionDate).HasColumnType("datetime");

                entity.Property(e => e.PrimaryReferralSourceNote).HasMaxLength(2500);

                entity.Property(e => e.Program).HasMaxLength(40);

                entity.Property(e => e.ReasonSeekingTreatment).HasMaxLength(2000);

                entity.Property(e => e.ReferralSourcedesc)
                    .HasMaxLength(800)
                    .IsUnicode(false);

                entity.Property(e => e.RegistrationMode)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.Sammsprogram)
                    .HasColumnName("SAMMSProgram")
                    .HasMaxLength(800)
                    .IsUnicode(false);

                entity.Property(e => e.Version).HasMaxLength(256);
            });

            modelBuilder.Entity<TblCowsV6>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Cowid })
                    .HasName("PK_COWS_V6");

                entity.ToTable("tbl_COWS_V6", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.Cowid).HasColumnName("COWID");

                entity.Property(e => e.AnxietyOrIrritabilitydesc)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.BoneOrJointAchesdesc)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.ClientSignatureDate).HasColumnType("datetime");

                entity.Property(e => e.CltId).HasColumnName("CltID");

                entity.Property(e => e.CompletedBy).HasMaxLength(1000);

                entity.Property(e => e.CreatedBy)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedOn).HasColumnType("datetime");

                entity.Property(e => e.Dttime)
                    .HasColumnType("datetime")
                    .HasColumnName("dttime");

                entity.Property(e => e.Giupset).HasColumnName("giupset");

                entity.Property(e => e.Giupsetdesc)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("GIUpsetdesc");

                entity.Property(e => e.GoosefleshSkindesc)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.PatientSignature).HasColumnType("ntext");

                entity.Property(e => e.Preadmissionid).HasColumnName("preadmissionid");

                entity.Property(e => e.PupilSizedesc)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.ReasonforthisAssessment)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("reasonforthisAssessment");

                entity.Property(e => e.RestingPulseRatedesc)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.Restlessnessdesc)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.RunnyNoseOrTearingdesc)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.StaffNameSignature)
                    .HasMaxLength(100)
                    .HasColumnName("staffNameSignature");

                entity.Property(e => e.Sweating).HasColumnName("sweating");

                entity.Property(e => e.Sweatingdesc)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.Tremor).HasColumnName("tremor");

                entity.Property(e => e.Tremordesc)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.UpdatedBy)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

                entity.Property(e => e.Version).HasMaxLength(25);

                entity.Property(e => e.Yawningdec)
                    .HasMaxLength(200)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TblCowxref>(entity =>
            {
                entity.HasKey(e => new { e.ColumnName, e.PermissibleValue })
                    .HasName("PK_CowXRef");

                entity.ToTable("tbl_COWXREF", "ctrl");

                entity.Property(e => e.ColumnName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.DescripiveText)
                    .IsRequired()
                    .HasMaxLength(200)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TblCustomAnswers>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.CaId, e.CaQid, e.CaCltid })
                    .HasName("PK_CustomAnswers");

                entity.ToTable("tbl_CustomAnswers", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.CaId).HasColumnName("caID");

                entity.Property(e => e.CaQid).HasColumnName("caQID");

                entity.Property(e => e.CaCltid).HasColumnName("caCLTID");

                entity.Property(e => e.CaAns)
                    .HasMaxLength(500)
                    .IsUnicode(false)
                    .HasColumnName("caANS");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<TblCustomQuestions>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.CId })
                    .HasName("PK_CustomQuestions");

                entity.ToTable("tbl_CustomQuestions", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.CId).HasColumnName("cID");

                entity.Property(e => e.CQuestion)
                    .HasMaxLength(250)
                    .IsUnicode(false)
                    .HasColumnName("cQuestion");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");
            });
            modelBuilder.Entity<VwTaskListMap>(entity =>
            {
                entity.ToView("vwTaskList", "tsk");

                entity.Property(e => e.ConStr).HasMaxLength(1045);

                entity.Property(e => e.Duration)
                    .IsRequired()
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.ErrorMessage)
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.LastModBy)
                    .IsRequired()
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(e => e.RunAt).HasColumnType("datetime");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.SortOrder)
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.TaskName)
                    .IsRequired()
                    .HasMaxLength(130)
                    .IsUnicode(false);

                entity.Property(e => e.WhereCondition)
                    .HasMaxLength(1000)
                    .IsUnicode(false);

                entity.Property(e => e.WorkDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<TblFormsCounts>(entity =>
            {
                entity.HasKey(e => new { e.FscDate, e.SiteCode, e.fscsid, e.fscCltID })
                    .HasName("PK_FormsCounts");

                entity.ToTable("tbl_FormsCounts", "stg");

                entity.Property(e => e.FscDate)
                    .HasColumnType("date")
                    .HasColumnName("fscDate");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Tbl3pElig>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.EId })
                    .HasName("PK_tbl3pElig");

                entity.ToTable("tbl_3pElig", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.EId).HasColumnName("eID");

                entity.Property(e => e.EClt).HasColumnName("eCLT");

                entity.Property(e => e.EDate)
                    .HasColumnName("eDATE")
                    .HasColumnType("date");

                entity.Property(e => e.EElecstatus)
                    .HasColumnName("eELECSTATUS")
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.EFormat)
                    .HasColumnName("eFormat")
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.EOrigid).HasColumnName("eORIGID");

                entity.Property(e => e.EPayer)
                    .HasColumnName("ePAYER")
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.EPost)
                    .HasColumnName("ePOST")
                    .IsUnicode(false);

                entity.Property(e => e.EResponse)
                    .HasColumnName("eRESPONSE")
                    .IsUnicode(false);

                entity.Property(e => e.EScan)
                    .HasColumnName("eSCAN")
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.EStaff)
                    .HasColumnName("eStaff")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.EStatus)
                    .HasColumnName("eStatus")
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.EstaffNote)
                    .HasColumnName("EStaffNote")
                    .IsUnicode(false);

                entity.Property(e => e.EstaffStatus)
                    .HasColumnName("EStaffSTATUS")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Filepath)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.Pyeligcheck)
                    .HasColumnName("pyeligcheck")
                    .HasColumnType("date");
            });

            modelBuilder.Entity<TblClinicalOpiateWithdrawalScale>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.FId })
                    .HasName("PK_ClinicalOpiateWithdrawalScale");

                entity.ToTable("tbl_ClinicalOpiateWithdrawalScale", "pats");

                entity.Property(e => e.FId).HasColumnName("fId");

                entity.Property(e => e.AnxNum)
                    .HasColumnName("AnxNUM")
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.AssessDate)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.AssesstimeText)
                    .HasColumnName("assesstimeTEXT")
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.BoneNum)
                    .HasColumnName("BoneNUM")
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.CombinedScore)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.CompletedName)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.FCltId).HasColumnName("fCltID");

                entity.Property(e => e.GenevaTest)
                    .HasColumnName("genevaTEST")
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.GiupsetNum)
                    .HasColumnName("GIUpsetNUM")
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.GooseNum)
                    .HasColumnName("GooseNUM")
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.PupilNum)
                    .HasColumnName("PupilNUM")
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.ReasonAssessList)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.RestingPulseNum)
                    .HasColumnName("RestingPulseNUM")
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.RestlessNum)
                    .HasColumnName("RestlessNUM")
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.RunnyNum)
                    .HasColumnName("RunnyNUM")
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.SweatNum)
                    .HasColumnName("SweatNUM")
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.TimeAmpm)
                    .HasColumnName("timeAMPM")
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.TremorNum)
                    .HasColumnName("TremorNUM")
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.YawnNum)
                    .HasColumnName("YawnNUM")
                    .HasMaxLength(200)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TblBriefAddictionMonitor>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.FId })
                    .HasName("PK_BreifAddictionMonitor");

                entity.ToTable("tbl_BriefAddictionMonitor", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.FId).HasColumnName("fId");

                entity.Property(e => e.AdminList)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("AdminLIST");

                entity.Property(e => e.ClinicianText)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("clinicianTEXT");

                entity.Property(e => e.Date)
                    .HasColumnType("date")
                    .HasColumnName("date");

                entity.Property(e => e.FClinic).HasColumnName("fClinic");

                entity.Property(e => e.FCltId).HasColumnName("fCltID");

                entity.Property(e => e.IntervalList)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("IntervalLIST");

                entity.Property(e => e.ProtectiveCalc)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("protectiveCALC");

                entity.Property(e => e.Q10Answer)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q10ANSWER");

                entity.Property(e => e.Q11Answer)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q11ANSWER");

                entity.Property(e => e.Q12Answer)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q12ANSWER");

                entity.Property(e => e.Q13Answer)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q13ANSWER");

                entity.Property(e => e.Q14Answer)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q14ANSWER");

                entity.Property(e => e.Q14Answer2)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q14ANSWER2");

                entity.Property(e => e.Q15Answer)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q15ANSWER");

                entity.Property(e => e.Q15Answer1)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q15ANSWER1");

                entity.Property(e => e.Q15Answer2)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q15ANSWER2");

                entity.Property(e => e.Q16Answer)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q16ANSWER");

                entity.Property(e => e.Q17Answer)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q17ANSWER");

                entity.Property(e => e.Q1Answer)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q1Answer");

                entity.Property(e => e.Q1answerList)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q1answerLIST");

                entity.Property(e => e.Q2Answer)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q2Answer");

                entity.Property(e => e.Q2answerList)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q2answerLIST");

                entity.Property(e => e.Q3answer)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q3answer");

                entity.Property(e => e.Q3answerList)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q3answerLIST");

                entity.Property(e => e.Q4answer)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q4answer");

                entity.Property(e => e.Q4answerList)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q4answerLIST");

                entity.Property(e => e.Q5answer)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q5answer");

                entity.Property(e => e.Q5answerList)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q5answerLIST");

                entity.Property(e => e.Q6AnswerList)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q6AnswerLIST");

                entity.Property(e => e.Q6answer)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q6answer");

                entity.Property(e => e.Q7aList)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q7aLIST");

                entity.Property(e => e.Q7answerNumeric)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q7answerNUMERIC");

                entity.Property(e => e.Q7bList)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q7bLIST");

                entity.Property(e => e.Q7cList)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q7cLIST");

                entity.Property(e => e.Q7dList)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q7dLIST");

                entity.Property(e => e.Q7eList)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q7eLIST");

                entity.Property(e => e.Q7fList)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q7fLIST");

                entity.Property(e => e.Q7gList)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q7gLIST");

                entity.Property(e => e.Q8Answer)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q8ANSWER");

                entity.Property(e => e.Q9Answer)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("q9ANSWER");

                entity.Property(e => e.RiskCalc)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("riskCALC");

                entity.Property(e => e.Test)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("test");

                entity.Property(e => e.UseCalc)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("useCALC");
            });

            modelBuilder.Entity<TblServices>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.SId });

                entity.ToTable("tbl_SERVICES", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.SId).HasColumnName("sID");

                entity.Property(e => e.BlAllowOverlap).HasColumnName("blAllowOverlap");

                entity.Property(e => e.OldArea)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("oldArea");

                entity.Property(e => e.OldSrv)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("oldSrv");

                entity.Property(e => e.SArea)
                    .HasMaxLength(250)
                    .IsUnicode(false)
                    .HasColumnName("sArea");

                entity.Property(e => e.SCost)
                    .HasColumnType("money")
                    .HasColumnName("sCost")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.SCptcode)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("sCPTCODE");

                entity.Property(e => e.SFilter).HasColumnName("sFilter");

                entity.Property(e => e.SReportBillable).HasColumnName("sReportBillable");

                entity.Property(e => e.SReqsig).HasColumnName("sREQSIG");

                entity.Property(e => e.SReqtime).HasColumnName("sREQTIME");

                entity.Property(e => e.SService)
                    .HasMaxLength(250)
                    .IsUnicode(false)
                    .HasColumnName("sSERVICE");

                entity.Property(e => e.STimeOnly).HasColumnName("sTimeOnly");
            });

            modelBuilder.Entity<TblConsents>(entity =>
            {
                entity.HasKey(e => e.Cid);

                entity.ToTable("tbl_CONSENTS", "ctrl");

                entity.Property(e => e.Cid)
                    .ValueGeneratedNever()
                    .HasColumnName("cid");

                entity.Property(e => e.Bac).HasColumnName("BAC");

                entity.Property(e => e.Blrecurr).HasColumnName("blrecurr");

                entity.Property(e => e.CDeleted).HasColumnName("cDeleted");

                entity.Property(e => e.CName)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("cNAME");

                entity.Property(e => e.Cdays).HasColumnName("cdays");

                entity.Property(e => e.ClientSig).HasColumnName("clientSig");

                entity.Property(e => e.IsMhform).HasColumnName("IsMHForm");

                entity.Property(e => e.NurseSig).HasColumnName("nurseSig");

                entity.Property(e => e.PhysicianSig).HasColumnName("physicianSig");

                entity.Property(e => e.StaffSig).HasColumnName("staffSig");

                entity.Property(e => e.SupervisorSig).HasColumnName("supervisorSig");
            });
            modelBuilder.Entity<TblConsents_Phc>(entity =>
            {
                entity.HasKey(e => e.Cid);

                entity.ToTable("tbl_CONSENTS_PHC", "ctrl");

                entity.Property(e => e.Cid)
                    .ValueGeneratedNever()
                    .HasColumnName("cid");

                entity.Property(e => e.Bac).HasColumnName("BAC");

                entity.Property(e => e.Blrecurr).HasColumnName("blrecurr");

                entity.Property(e => e.CDeleted).HasColumnName("cDeleted");

                entity.Property(e => e.CName)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("cNAME");

                entity.Property(e => e.Cdays).HasColumnName("cdays");

                entity.Property(e => e.ClientSig).HasColumnName("clientSig");

                entity.Property(e => e.IsMhform).HasColumnName("IsMHForm");

                entity.Property(e => e.NurseSig).HasColumnName("nurseSig");

                entity.Property(e => e.PhysicianSig).HasColumnName("physicianSig");

                entity.Property(e => e.StaffSig).HasColumnName("staffSig");

                entity.Property(e => e.SupervisorSig).HasColumnName("supervisorSig");
            });
            modelBuilder.Entity<TblForms2Process>(entity =>
            {
                entity.HasKey(e => new { e.FormProcessId, e.FormName })
                    .HasName("PK_Forms2Process");

                entity.ToTable("tbl_Forms2Process", "ctrl");

                entity.Property(e => e.FormProcessId).ValueGeneratedOnAdd();

                entity.Property(e => e.FormName)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.CompletedBy)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Counselor)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Doctor)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.MedicalProvider)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Patient)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Provider)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Requestor)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Staff)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Supervisor)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.TableName)
                    .HasMaxLength(200)
                    .IsUnicode(false);
            });
            modelBuilder.Entity<TblFmp>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.FmpId })
                    .HasName("PK_tbl_FMT");

                entity.ToTable("tbl_FMP", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.FmpId).HasColumnName("fmpID");

                entity.Property(e => e.AtriskType)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("atriskTYPE");

                entity.Property(e => e.FmPendtext)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("fmPENDTEXT");

                entity.Property(e => e.FmpDtAdded)
                    .HasColumnType("datetime")
                    .HasColumnName("fmpDtAdded");

                entity.Property(e => e.FmpDtEnd)
                    .HasColumnType("datetime")
                    .HasColumnName("fmpDtEnd");

                entity.Property(e => e.FmpDtEnded)
                    .HasColumnType("datetime")
                    .HasColumnName("fmpDtEnded");

                entity.Property(e => e.FmpDtProjEnd)
                    .HasColumnType("datetime")
                    .HasColumnName("fmpDtProjEnd");

                entity.Property(e => e.FmpDtStart)
                    .HasColumnType("datetime")
                    .HasColumnName("fmpDtStart");

                entity.Property(e => e.FmpIntRate).HasColumnName("fmpIntRate");

                entity.Property(e => e.FmpLngClt).HasColumnName("fmpLngClt");

                entity.Property(e => e.FmpStrDesc)
                    .HasMaxLength(250)
                    .IsUnicode(false)
                    .HasColumnName("fmpStrDesc");

                entity.Property(e => e.FmpStrReason)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("fmpStrReason");

                entity.Property(e => e.FmpStrUserAdded)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("fmpStrUserAdded");

                entity.Property(e => e.FmpStrUserEnded)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("fmpStrUserEnded");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<TblDboFormQuestionAnswers>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.FormName, e.FormId, e.ClientId, e.QuestionId, e.QuestionOrderId })
                    .HasName("PK_DBO_FormQuestionAnswers");

                entity.ToTable("tbl_dbo_FormQuestionAnswers", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.FormName)
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.FormId)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.QuestionOrderId).IsUnicode(false);

                entity.Property(e => e.AnswerValue).IsUnicode(false);

                entity.Property(e => e.CreatedBy).IsUnicode(false);

                entity.Property(e => e.CreatedOn).HasColumnType("date");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.OptionId).IsUnicode(false);

                entity.Property(e => e.QuestionText).IsUnicode(false);

                entity.Property(e => e.UpdatedBy).IsUnicode(false);

                entity.Property(e => e.UpdatedOn).HasColumnType("date");
            });

            modelBuilder.Entity<TblDboFormAnswerSignatures>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.FormName, e.FormId, e.ClientId })
                    .HasName("PK_FormAnswerSignatures");

                entity.ToTable("tbl_dbo_FormAnswerSignatures", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.FormName)
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.FormId)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.CompletedBySignatureSignatureDate).HasColumnType("datetime");

                entity.Property(e => e.CounselorSignatureSignatureDate).HasColumnType("datetime");

                entity.Property(e => e.CreatedOn).HasColumnType("date");

                entity.Property(e => e.DoctorSignatureSignatureDate).HasColumnType("datetime");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.MedicalProviderSignatureSignatureDate).HasColumnType("datetime");

                entity.Property(e => e.PatientSignatureDate).HasColumnType("datetime");

                entity.Property(e => e.ProviderSignatureSignatureDate).HasColumnType("datetime");

                entity.Property(e => e.RequestorSignatureDate).HasColumnType("datetime");

                entity.Property(e => e.StaffSignatureDate).HasColumnType("datetime");

                entity.Property(e => e.SupervisorSignatureSignatureDate).HasColumnType("datetime");

                entity.Property(e => e.UpdatedOn).HasColumnType("date");
            });

            modelBuilder.Entity<TblDboAnswerSignatures>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.FormId, e.SignatureId })
                    .HasName("PK_AnswerSignatures");

                entity.ToTable("tbl_dbo_AnswerSignatures", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.FormId)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.DataField)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.DateField)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.ImageField)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.SignatureName)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.SignedDateTime).HasColumnType("datetime");
            });

            modelBuilder.Entity<TblDose>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.DoseId })
                    .HasName("PK_Dose");

                entity.ToTable("tbl_DOSE", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.CltId).HasColumnName("CltID");

                entity.Property(e => e.DtMedDate)
                    .HasColumnName("dtMedDate")
                    .HasColumnType("datetime");

                entity.Property(e => e.DoseId).HasColumnName("DoseID");

                entity.Property(e => e.BlBulk).HasColumnName("blBULK");

                entity.Property(e => e.BlException).HasColumnName("blException");

                entity.Property(e => e.BlPrepack).HasColumnName("blPrepack");

                entity.Property(e => e.BlVoid).HasColumnName("blVoid");

                entity.Property(e => e.Bottletype)
                    .HasColumnName("bottletype")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Dosenote)
                    .HasColumnName("dosenote")
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.Dosesig)
                    .HasColumnName("dosesig")
                    .HasColumnType("ntext");

                entity.Property(e => e.DtDate)
                    .HasColumnName("dtDate")
                    .HasColumnType("datetime");

                entity.Property(e => e.DtVoid).HasColumnName("dtVoid");

                entity.Property(e => e.Dtgiven)
                    .HasColumnName("DTgiven")
                    .HasColumnType("datetime");

                entity.Property(e => e.Dtprep)
                    .HasColumnName("DTprep")
                    .HasColumnType("datetime");

                entity.Property(e => e.ExceptionReason)
                    .HasMaxLength(1250)
                    .IsUnicode(false);

                entity.Property(e => e.Exceptiontype)
                    .HasColumnName("exceptiontype")
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.GuestId).HasColumnName("GuestID");

                entity.Property(e => e.InventoryGroup)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Manualauthdtm)
                    .HasColumnName("manualauthdtm")
                    .HasColumnType("datetime");

                entity.Property(e => e.Manualauthuser)
                    .HasColumnName("manualauthuser")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Ordernum).HasColumnName("ordernum");

                entity.Property(e => e.Ppstaff)
                    .HasColumnName("PPStaff")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.SiteId).HasColumnName("SiteID");

                entity.Property(e => e.StrUser)
                    .HasColumnName("strUser")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.StrVoidReason)
                    .HasColumnName("strVoidReason")
                    .HasMaxLength(1000)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TblDoseExcuse>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.ExId })
                    .HasName("PK_DoseExcuse");

                entity.ToTable("tbl_DOSE_Excuse", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.ExId).HasColumnName("ExID");

                entity.Property(e => e.CltId).HasColumnName("cltID");

                entity.Property(e => e.DtEx)
                    .HasColumnName("dtEX")
                    .HasColumnType("datetime");

                entity.Property(e => e.Dtstamp)
                    .HasColumnName("dtstamp")
                    .HasColumnType("datetime");

                entity.Property(e => e.StrExcused)
                    .HasColumnName("strEXCUSED")
                    .HasColumnType("ntext");

                entity.Property(e => e.StrUser)
                    .HasColumnName("strUSER")
                    .HasMaxLength(100);
            });

            modelBuilder.Entity<YearlyAuditData>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("YearlyAuditData", "tsk");

                entity.Property(e => e.ClinicName).HasMaxLength(255);

                entity.Property(e => e.CompKey).HasMaxLength(619);

                entity.Property(e => e.ConName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ConStr).HasMaxLength(1045);

                entity.Property(e => e.ConType)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.ConnectionId).HasColumnName("ConnectionID");

                entity.Property(e => e.ContractDate).HasColumnType("date");

                entity.Property(e => e.CtrlMethod)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.DateField)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.DbName)
                    .HasColumnName("dbName")
                    .HasMaxLength(255);

                entity.Property(e => e.DsnSchema)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.DsnTbl)
                    .IsRequired()
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.EnrollCutoff).HasColumnType("date");

                entity.Property(e => e.FromTblVw)
                    .IsRequired()
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.SiteCode).HasMaxLength(25);

                entity.Property(e => e.SortOrder)
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.SrcSchema)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.WhereCondition)
                    .IsRequired()
                    .HasMaxLength(1000)
                    .IsUnicode(false);

                entity.Property(e => e.WrkYear)
                    .HasMaxLength(10)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TblPbi3Payauth>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.TpaId })
                    .HasName("PK_pbi3PayAuth");

                entity.ToTable("tbl_pbi3PAYauth", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.TpaId).HasColumnName("tpaID");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.PayerGroup)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.PayerType)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ProgGroup)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.TpAuthpath)
                    .HasColumnName("tpAUTHPATH")
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.TpConfirmpath)
                    .HasColumnName("tpCONFIRMPath")
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.TpEffdate)
                    .HasColumnName("tpEFFDate")
                    .HasColumnType("date");

                entity.Property(e => e.TpFail)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.TpNote)
                    .HasColumnName("tpNOTE")
                    .IsUnicode(false);

                entity.Property(e => e.TpRequestForm)
                    .HasColumnName("tpRequestForm")
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.TpResponseForm)
                    .HasColumnName("tpResponseForm")
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.TpServ)
                    .HasColumnName("tpServ")
                    .HasMaxLength(300)
                    .IsUnicode(false);

                entity.Property(e => e.TpServapproved)
                    .HasColumnName("tpSERVAPPROVED")
                    .HasMaxLength(1500)
                    .IsUnicode(false);

                entity.Property(e => e.TpTermDate)
                    .HasColumnName("tpTermDate")
                    .HasColumnType("date");

                entity.Property(e => e.TpType)
                    .HasColumnName("tpTYPE")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.TpUnits).HasColumnName("tpUNITS");

                entity.Property(e => e.TpaAuthCode)
                    .HasColumnName("tpaAuthCode")
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.TpaBigKey)
                    .HasColumnName("tpaBigKey")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.TpaCltid).HasColumnName("tpaCLTID");

                entity.Property(e => e.TpaCompKey)
                    .HasColumnName("tpaCompKey")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.TpaDesc)
                    .HasColumnName("tpaDESC")
                    .HasColumnType("ntext");

                entity.Property(e => e.TpaEffDate)
                    .HasColumnName("tpaEffDATE")
                    .HasColumnType("datetime");

                entity.Property(e => e.TpaPayer)
                    .HasColumnName("tpaPayer")
                    .HasMaxLength(100);

                entity.Property(e => e.TpaStaff)
                    .HasColumnName("tpaSTAFF")
                    .HasMaxLength(50);

                entity.Property(e => e.TpaTermDate)
                    .HasColumnName("tpaTermDATE")
                    .HasColumnType("datetime");

                entity.Property(e => e.Tpadt)
                    .HasColumnName("tpadt")
                    .HasColumnType("datetime");
            });

            modelBuilder.Entity<TblBills>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.BillId })
                .HasName("PK_Bills")
                .IsClustered(false);

                entity.HasIndex(e => new { e.SiteCode, e.BillDate })
                .HasName("idx_Bills_update");

                entity.HasIndex(e => new { e.BillAdjust, e.BillAdjustid, e.BillAptId, e.BillBill, e.BillCltid, e.BillGuestId, e.BillId, e.BillOrgdt, e.BillPay, e.BillPaytype, e.BillReason, e.BillReceiptNum, e.BillServId, e.BillSiteId, e.BlnDeposit, e.Costcenter, e.DtDeposit, e.Fifoallocated, e.Fifobalance, e.LastModAt, e.RowChkSum, e.StrUser, e.SiteCode, e.BillDate })
                    .HasName("nci_wi_tbl_Bills_029D3D873203D107D483DFE298610835");

                entity.Property(e => e.BillPaytype).IsUnicode(false);

                entity.Property(e => e.BillReason).IsUnicode(false);

                entity.Property(e => e.Costcenter).IsUnicode(false);

                entity.Property(e => e.StrUser).IsUnicode(false);
            });

            modelBuilder.Entity<Tblvw3pBillSub>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.DsId, e.PyPayerid, e.PySubsid, e.PyGroup, e.CptMod, e.Charge });

                entity.ToTable("tbl_vw3pBillSub", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.DsId).HasColumnName("dsID");

                entity.Property(e => e.BillUnits).HasColumnName("billUnits");

                entity.Property(e => e.Billdatecriteria)
                    .HasColumnType("datetime")
                    .HasColumnName("billdatecriteria");

                entity.Property(e => e.Charge).HasColumnName("charge");

                entity.Property(e => e.Clientname)
                    .HasMaxLength(152)
                    .IsUnicode(false)
                    .HasColumnName("clientname");

                entity.Property(e => e.CltAdd1)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("cltADD1");

                entity.Property(e => e.CltCity)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("cltCity");

                entity.Property(e => e.CltDob)
                    .HasColumnType("datetime")
                    .HasColumnName("cltDOB");

                entity.Property(e => e.CltGender)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("cltGender");

                entity.Property(e => e.CltM4id)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("cltM4ID");

                entity.Property(e => e.CltMarry)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("cltMARRY");

                entity.Property(e => e.CltPhone)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("cltPhone");

                entity.Property(e => e.CltState)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("cltState");

                entity.Property(e => e.Cltzip)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("cltzip");

                entity.Property(e => e.Cptcode)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("CPTCODE");

                entity.Property(e => e.Descript)
                    .IsRequired()
                    .HasMaxLength(17)
                    .IsUnicode(false)
                    .HasColumnName("descript");

                entity.Property(e => e.DsClt).HasColumnName("dsClt");

                entity.Property(e => e.DsDtEnd)
                    .HasColumnType("datetime")
                    .HasColumnName("dsDtEnd");

                entity.Property(e => e.DsDtStart)
                    .HasColumnType("datetime")
                    .HasColumnName("dsDtStart");

                entity.Property(e => e.DsPos)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("dsPOS");

                entity.Property(e => e.DsTxtSrv)
                    .HasMaxLength(100)
                    .HasColumnName("dsTxtSrv");

                entity.Property(e => e.DsTxtType)
                    .HasMaxLength(50)
                    .HasColumnName("dsTxtType");

                entity.Property(e => e.Dsarea)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("dsarea");

                entity.Property(e => e.Dsbilled)
                    .HasColumnType("datetime")
                    .HasColumnName("DSbilled");

                entity.Property(e => e.DsdblUnits).HasColumnName("dsdblUnits");

                entity.Property(e => e.Dsdiag)
                    .IsRequired()
                    .HasMaxLength(5)
                    .IsUnicode(false)
                    .HasColumnName("dsdiag");

                entity.Property(e => e.DstxtStaff)
                    .HasMaxLength(100)
                    .HasColumnName("dstxtStaff");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.Mg).HasColumnName("MG");

                entity.Property(e => e.Modifier)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Ndc)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("NDC");

                entity.Property(e => e.Npi)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("npi");

                entity.Property(e => e.PayDefaultsubmit)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("payDEFAULTSUBMIT");

                entity.Property(e => e.Payclass)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("payclass");

                entity.Property(e => e.PyGroup)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("pyGROUP");

                entity.Property(e => e.PyPayerid)
                    .HasMaxLength(75)
                    .IsUnicode(false)
                    .HasColumnName("pyPAYERID");

                entity.Property(e => e.PySubsid)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("pySUBSID");

                entity.Property(e => e.ScrubError)
                    .IsRequired()
                    .HasMaxLength(1000)
                    .IsUnicode(false);

                entity.Property(e => e.SiteId).HasColumnName("SiteID");

                entity.Property(e => e.TpaAuthCode)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("tpaAuthCode");
            });

            modelBuilder.Entity<TblCheckIn>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.CiDate, e.CiId })
                    .HasName("PK_CheckIn");

                entity.HasIndex(e => new { e.CiAmt, e.CiCltid, e.Cicltm4id, e.CicltName, e.CiCode, e.CiDoses, e.CiHold, e.CiQueue, e.CiServeddtm, e.CiServedStaff, e.CiTime, e.CiUser, e.LastModAt, e.MinutesWaited, e.RowChkSum, e.CiDate, e.CiId })
                    .HasName("nci_wi_tbl_CheckIn_UpLoad_Idx");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.CiCltid).IsUnicode(false);

                entity.Property(e => e.CiCode).IsUnicode(false);

                entity.Property(e => e.CiQueue).IsUnicode(false);

                entity.Property(e => e.CiServedStaff).IsUnicode(false);

                entity.Property(e => e.CiUser).IsUnicode(false);

                entity.Property(e => e.CicltName).IsUnicode(false);

                entity.Property(e => e.Cicltm4id).IsUnicode(false);
            });

            modelBuilder.Entity<TblClaimLineItem>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.TpcliId })
                    .HasName("PK_ClaimLineItems");

                entity.HasIndex(e => new { e.SiteCode, e.TpcliDtmAdded })
                    .HasName("idx_ClaimLineItem_update");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.TpcliDbnotes).IsUnicode(false);

                entity.Property(e => e.TpcliDiagnosis).IsUnicode(false);

                entity.Property(e => e.TpcliPayerClaimId).IsUnicode(false);

                entity.Property(e => e.TpcliProviderId).IsUnicode(false);

                entity.Property(e => e.TpcliStrAdded).IsUnicode(false);

                entity.Property(e => e.TpcliStrCpt).IsUnicode(false);

                entity.Property(e => e.TpcliStrModifier).IsUnicode(false);

                entity.Property(e => e.TpcliStrNdc).IsUnicode(false);

                entity.Property(e => e.TpcliStrPos).IsUnicode(false);

                entity.Property(e => e.TpcliTxtService).IsUnicode(false);

                entity.Property(e => e.TpclivoidUser).IsUnicode(false);
            });

            modelBuilder.Entity<TblClaimLineItemActivity>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.LiaId })
                    .HasName("PK_ClaimLineItemActivity");

                entity.HasIndex(e => new { e.SiteCode, e.LiaDtm })
                    .HasName("idx_ClaimLineItemActivity");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.LiaAction1).IsUnicode(false);

                entity.Property(e => e.LiaAction2).IsUnicode(false);

                entity.Property(e => e.LiaAdjcontract).IsUnicode(false);

                entity.Property(e => e.LiaAdjgeneral).IsUnicode(false);

                entity.Property(e => e.LiaAdjreason).IsUnicode(false);

                entity.Property(e => e.LiaAnsi1).IsUnicode(false);

                entity.Property(e => e.LiaAnsi2).IsUnicode(false);

                entity.Property(e => e.LiaAnsimod1).IsUnicode(false);

                entity.Property(e => e.LiaAnsimod2).IsUnicode(false);

                entity.Property(e => e.LiaDbnotes).IsUnicode(false);

                entity.Property(e => e.LiaStrDesc).IsUnicode(false);

                entity.Property(e => e.LiaStrUser).IsUnicode(false);

                entity.Property(e => e.Liastrtext).IsUnicode(false);
            });

            modelBuilder.Entity<TblClaims>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.TpcId })
                    .HasName("PK_Claims");

                entity.HasIndex(e => new { e.SiteCode, e.TpcCreatedDate })
                    .HasName("idx_Claims_Update");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.F10auto).IsUnicode(false);

                entity.Property(e => e.F10employ).IsUnicode(false);

                entity.Property(e => e.F10local).IsUnicode(false);

                entity.Property(e => e.F10oth).IsUnicode(false);

                entity.Property(e => e.F11insanother).IsUnicode(false);

                entity.Property(e => e.F11insdob).IsUnicode(false);

                entity.Property(e => e.F11insemploy).IsUnicode(false);

                entity.Property(e => e.F11insnumber).IsUnicode(false);

                entity.Property(e => e.F11insplan).IsUnicode(false);

                entity.Property(e => e.F11inssex).IsUnicode(false);

                entity.Property(e => e.F12sig).IsUnicode(false);

                entity.Property(e => e.F12sigdate).IsUnicode(false);

                entity.Property(e => e.F13inssig).IsUnicode(false);

                entity.Property(e => e.F14date).IsUnicode(false);

                entity.Property(e => e.F15firstdate).IsUnicode(false);

                entity.Property(e => e.F16dateunableend).IsUnicode(false);

                entity.Property(e => e.F16dateunablestart).IsUnicode(false);

                entity.Property(e => e.F17refername).IsUnicode(false);

                entity.Property(e => e.F17refernpi).IsUnicode(false);

                entity.Property(e => e.F18datehospend).IsUnicode(false);

                entity.Property(e => e.F18datehospstart).IsUnicode(false);

                entity.Property(e => e.F19local).IsUnicode(false);

                entity.Property(e => e.F1id).IsUnicode(false);

                entity.Property(e => e.F20outsidelab).IsUnicode(false);

                entity.Property(e => e.F21diag1).IsUnicode(false);

                entity.Property(e => e.F21diag2).IsUnicode(false);

                entity.Property(e => e.F21diag3).IsUnicode(false);

                entity.Property(e => e.F21diag4).IsUnicode(false);

                entity.Property(e => e.F22medresub).IsUnicode(false);

                entity.Property(e => e.F23priorauth).IsUnicode(false);

                entity.Property(e => e.F25taxid).IsUnicode(false);

                entity.Property(e => e.F26account).IsUnicode(false);

                entity.Property(e => e.F27assign).IsUnicode(false);

                entity.Property(e => e.F28totalcharge).IsUnicode(false);

                entity.Property(e => e.F29amtpaid).IsUnicode(false);

                entity.Property(e => e.F2name).IsUnicode(false);

                entity.Property(e => e.F30balancedue).IsUnicode(false);

                entity.Property(e => e.F31date).IsUnicode(false);

                entity.Property(e => e.F31phys).IsUnicode(false);

                entity.Property(e => e.F32a).IsUnicode(false);

                entity.Property(e => e.F32b).IsUnicode(false);

                entity.Property(e => e.F32line1).IsUnicode(false);

                entity.Property(e => e.F32line2).IsUnicode(false);

                entity.Property(e => e.F32line3).IsUnicode(false);

                entity.Property(e => e.F32line4).IsUnicode(false);

                entity.Property(e => e.F33a).IsUnicode(false);

                entity.Property(e => e.F33b).IsUnicode(false);

                entity.Property(e => e.F33line1).IsUnicode(false);

                entity.Property(e => e.F33line2).IsUnicode(false);

                entity.Property(e => e.F33line3).IsUnicode(false);

                entity.Property(e => e.F33line4).IsUnicode(false);

                entity.Property(e => e.F33phone).IsUnicode(false);

                entity.Property(e => e.F3dob).IsUnicode(false);

                entity.Property(e => e.F3sex).IsUnicode(false);

                entity.Property(e => e.F4insname).IsUnicode(false);

                entity.Property(e => e.F5add).IsUnicode(false);

                entity.Property(e => e.F5city).IsUnicode(false);

                entity.Property(e => e.F5phone).IsUnicode(false);

                entity.Property(e => e.F5state).IsUnicode(false);

                entity.Property(e => e.F5zip).IsUnicode(false);

                entity.Property(e => e.F6insrel).IsUnicode(false);

                entity.Property(e => e.F7insadd).IsUnicode(false);

                entity.Property(e => e.F7inscity).IsUnicode(false);

                entity.Property(e => e.F7insphone).IsUnicode(false);

                entity.Property(e => e.F7insstate).IsUnicode(false);

                entity.Property(e => e.F7inszip).IsUnicode(false);

                entity.Property(e => e.F8stat).IsUnicode(false);

                entity.Property(e => e.F9othinsdob).IsUnicode(false);

                entity.Property(e => e.F9othinsemp).IsUnicode(false);

                entity.Property(e => e.F9othinsname).IsUnicode(false);

                entity.Property(e => e.F9othinsnumber).IsUnicode(false);

                entity.Property(e => e.F9othinsplan).IsUnicode(false);

                entity.Property(e => e.F9othinssex).IsUnicode(false);

                entity.Property(e => e.TpcDbnotes).IsUnicode(false);

                entity.Property(e => e.TpcEncounter).IsFixedLength();

                entity.Property(e => e.TpcPayerCin).IsUnicode(false);

                entity.Property(e => e.TpcRebillreason).IsUnicode(false);

                entity.Property(e => e.TpcReferring).IsUnicode(false);

                entity.Property(e => e.TpcSrvType).IsUnicode(false);

                entity.Property(e => e.TpcStrAdded).IsUnicode(false);

                entity.Property(e => e.TpcStrPayer).IsUnicode(false);

                entity.Property(e => e.TpcStrPrimary).IsUnicode(false);

                entity.Property(e => e.TpcStrStatus).IsUnicode(false);

                entity.Property(e => e.TpcStrWeek).IsUnicode(false);
            });

            modelBuilder.Entity<TblClientDemo1>(entity =>
            {
                entity.HasKey(e => new { e.PrimKey })
                    .HasName("PK_tblClientDemo1");

                entity.Property(e => e.Address1).IsUnicode(false);

                entity.Property(e => e.Address2).IsUnicode(false);

                entity.Property(e => e.City).IsUnicode(false);

                entity.Property(e => e.ClientM4id).IsUnicode(false);

                entity.Property(e => e.County).IsUnicode(false);

                entity.Property(e => e.Education).IsUnicode(false);

                entity.Property(e => e.Email).IsUnicode(false);

                entity.Property(e => e.EmpStatus).IsUnicode(false);

                entity.Property(e => e.Employer).IsUnicode(false);

                entity.Property(e => e.Eye).IsUnicode(false);

                entity.Property(e => e.FirstName).IsUnicode(false);

                entity.Property(e => e.Gender).IsUnicode(false);

                entity.Property(e => e.Hair).IsUnicode(false);

                entity.Property(e => e.Height).IsUnicode(false);

                entity.Property(e => e.Income).IsUnicode(false);

                entity.Property(e => e.Language).IsUnicode(false);

                entity.Property(e => e.LastName).IsUnicode(false);

                entity.Property(e => e.Marital).IsUnicode(false);

                entity.Property(e => e.MiddleName).IsUnicode(false);

                entity.Property(e => e.Phone).IsUnicode(false);

                entity.Property(e => e.Race).IsUnicode(false);

                entity.Property(e => e.Ssn).IsUnicode(false);

                entity.Property(e => e.State).IsUnicode(false);

                entity.Property(e => e.Suffix).IsUnicode(false);

                entity.Property(e => e.Weight).IsUnicode(false);

                entity.Property(e => e.WorkPhone).IsUnicode(false);

                entity.Property(e => e.Zip).IsUnicode(false);
            });

            modelBuilder.Entity<TblClientDemo2>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.ClientId })
                    .HasName("PK_tblClientDemo2");

                entity.Property(e => e.Amount).IsUnicode(false);

                entity.Property(e => e.Amsid).IsUnicode(false);

                entity.Property(e => e.Changeuser).IsUnicode(false);

                entity.Property(e => e.Clt3pBack).IsUnicode(false);

                entity.Property(e => e.Clt3pfront).IsUnicode(false);

                entity.Property(e => e.Clt911Name).IsUnicode(false);

                entity.Property(e => e.Clt911Ph).IsUnicode(false);

                entity.Property(e => e.Clt911Relation).IsUnicode(false);

                entity.Property(e => e.Counselor).IsUnicode(false);

                entity.Property(e => e.Dow1).IsUnicode(false);

                entity.Property(e => e.Dow2).IsUnicode(false);

                entity.Property(e => e.DtLastUa).IsUnicode(false);

                entity.Property(e => e.Eth).IsUnicode(false);

                entity.Property(e => e.Freq).IsUnicode(false);

                entity.Property(e => e.Ins).IsUnicode(false);

                entity.Property(e => e.NurseNotes).IsUnicode(false);

                entity.Property(e => e.Panel).IsUnicode(false);

                entity.Property(e => e.Payday).IsUnicode(false);

                entity.Property(e => e.Picpath).IsUnicode(false);

                entity.Property(e => e.Prog).IsUnicode(false);

                entity.Property(e => e.Rin).IsUnicode(false);

                entity.Property(e => e.Risk).IsUnicode(false);

                entity.Property(e => e.Special).IsUnicode(false);

                entity.Property(e => e.Status).IsUnicode(false);
            });

            modelBuilder.Entity<TblClinic>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Pkey })
                    .HasName("PK_tbl_CLINIC");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.AdDomain).IsUnicode(false);

                entity.Property(e => e.AdjustmentEmail).IsUnicode(false);

                entity.Property(e => e.AutoSetLabelprinter).IsUnicode(false);

                entity.Property(e => e.AutoSetReceiptPrinter).IsUnicode(false);

                entity.Property(e => e.BillDirection).IsUnicode(false);

                entity.Property(e => e.Chsamsid).IsUnicode(false);

                entity.Property(e => e.ClaimDir).IsUnicode(false);

                entity.Property(e => e.ClinicLetter).IsUnicode(false);

                entity.Property(e => e.ClinicName).IsUnicode(false);

                entity.Property(e => e.ClinicState).IsUnicode(false);

                entity.Property(e => e.DefaultProgram).IsUnicode(false);

                entity.Property(e => e.DictionaryPath).IsUnicode(false);

                entity.Property(e => e.DiversionType).IsUnicode(false);

                entity.Property(e => e.DocTemplatePath).IsUnicode(false);

                entity.Property(e => e.Grammerpath).IsUnicode(false);

                entity.Property(e => e.Helpfile).IsUnicode(false);

                entity.Property(e => e.HnPurl).IsUnicode(false);

                entity.Property(e => e.Iispath).IsUnicode(false);

                entity.Property(e => e.IntakePacketUrl).IsUnicode(false);

                entity.Property(e => e.LabAcct).IsUnicode(false);

                entity.Property(e => e.OtherInvType).IsUnicode(false);

                entity.Property(e => e.ReportDir).IsUnicode(false);

                entity.Property(e => e.ReportServer).IsUnicode(false);

                entity.Property(e => e.ScanPath).IsUnicode(false);

                entity.Property(e => e.SigImgpath).IsUnicode(false);

                entity.Property(e => e.SigImguri).IsUnicode(false);

                entity.Property(e => e.ToxAcct).IsUnicode(false);

                entity.Property(e => e.ToxProvider).IsUnicode(false);

                entity.Property(e => e.ToxTixspecial).IsUnicode(false);

                entity.Property(e => e.Uapath).IsUnicode(false);

                entity.Property(e => e.Urlassessment).IsUnicode(false);

                entity.Property(e => e.Voregistrationpath).IsUnicode(false);

                entity.Property(e => e.Wordpath).IsUnicode(false);
            });

            modelBuilder.Entity<TblCodes>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.CdeId })
                    .HasName("PK_Codes");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.Cde3pPosoverride).IsUnicode(false);

                entity.Property(e => e.CdeDesc).IsUnicode(false);

                entity.Property(e => e.CdeDischargeType).IsUnicode(false);

                entity.Property(e => e.CdeFund).IsUnicode(false);

                entity.Property(e => e.CdeGroup).IsUnicode(false);

                entity.Property(e => e.CdeModality).IsUnicode(false);

                entity.Property(e => e.CdeProvider).IsUnicode(false);

                entity.Property(e => e.CdeServiceSetting).IsUnicode(false);

                entity.Property(e => e.CdeSiteNum).IsUnicode(false);

                entity.Property(e => e.Cdelblcolor).IsUnicode(false);

                entity.Property(e => e.LastModAt).HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<TblConnections>(entity =>
            {
                entity.HasKey(e => e.ConId)
                    .HasName("PK_Connections");

                entity.Property(e => e.ConName).IsUnicode(false);

                entity.Property(e => e.ConStr).IsUnicode(false);

                entity.Property(e => e.LastModBy).IsUnicode(false);

                entity.Property(e => e.Password).IsUnicode(false);

                entity.Property(e => e.UserName).IsUnicode(false);
            });

            modelBuilder.Entity<TblCtrlSiteTableInit>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.TableName })
                    .HasName("PK_CtrlSiteTableInit");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.TableName).IsUnicode(false);

                entity.Property(e => e.DateField).IsUnicode(false);
            });

            modelBuilder.Entity<TblCtrlSiteTableInitLog>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.TableName, e.WorkDate })
                    .HasName("PK_CtrlSiteTableInitLog");

                entity.HasIndex(e => e.WorkDate)
                    .HasName("nci_wi_tbl_CtrlSiteTableInitLog_AA9A72B85556F52B1103DA2BC039CA18");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.TableName).IsUnicode(false);
            });

            modelBuilder.Entity<TblDartsSrv>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.DsId })
                    .HasName("PK_DartsSrv")
                    .IsClustered(false);

                entity.HasIndex(e => new { e.SiteCode, e.Dsbilled, e.DsUpdate, e.DsDtAdded, e.DsDtStart, e.DsDtEnd })
                    .HasName("idx_DartsSrv_Update");

                entity.Property(e => e.DsArea).IsUnicode(false);

                entity.Property(e => e.DsDbnotes).IsUnicode(false);

                entity.Property(e => e.DsDiag).IsUnicode(false);

                entity.Property(e => e.DsDiag10).IsUnicode(false);

                entity.Property(e => e.DsError).IsUnicode(false);

                entity.Property(e => e.DsSigUser).IsUnicode(false);

                entity.Property(e => e.DsSigUserCosign).IsUnicode(false);

                entity.Property(e => e.DsSigcltuser).IsUnicode(false);

                entity.Property(e => e.DsTxtHiv).IsUnicode(false);

                entity.Property(e => e.DsUpdatestaff).IsUnicode(false);

                //entity.Property(e => e.UpsizeTs)
                //    .IsRowVersion()
                //    .IsConcurrencyToken();
            });
            modelBuilder.Entity<TblDartsSrv_2015>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.DsId })
                    .HasName("PK_DartsSrv_2015")
                    .IsClustered(false);

                entity.HasIndex(e => new { e.SiteCode, e.Dsbilled, e.DsUpdate, e.DsDtAdded, e.DsDtStart, e.DsDtEnd })
                    .HasName("idx_DartsSrv_Update_2015");

                entity.Property(e => e.DsArea).IsUnicode(false);

                entity.Property(e => e.DsDbnotes).IsUnicode(false);

                entity.Property(e => e.DsDiag).IsUnicode(false);

                entity.Property(e => e.DsDiag10).IsUnicode(false);

                entity.Property(e => e.DsError).IsUnicode(false);

                entity.Property(e => e.DsSigUser).IsUnicode(false);

                entity.Property(e => e.DsSigUserCosign).IsUnicode(false);

                entity.Property(e => e.DsSigcltuser).IsUnicode(false);

                entity.Property(e => e.DsTxtHiv).IsUnicode(false);

                entity.Property(e => e.DsUpdatestaff).IsUnicode(false);

                //entity.Property(e => e.UpsizeTs)
                //    .IsRowVersion()
                //    .IsConcurrencyToken();
            });
            modelBuilder.Entity<TblDartsSrv_2016>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.DsId })
                    .HasName("PK_DartsSrv_2016")
                    .IsClustered(false);

                entity.HasIndex(e => new { e.SiteCode, e.Dsbilled, e.DsUpdate, e.DsDtAdded, e.DsDtStart, e.DsDtEnd })
                    .HasName("idx_DartsSrv_Update_2016");

                entity.Property(e => e.DsArea).IsUnicode(false);

                entity.Property(e => e.DsDbnotes).IsUnicode(false);

                entity.Property(e => e.DsDiag).IsUnicode(false);

                entity.Property(e => e.DsDiag10).IsUnicode(false);

                entity.Property(e => e.DsError).IsUnicode(false);

                entity.Property(e => e.DsSigUser).IsUnicode(false);

                entity.Property(e => e.DsSigUserCosign).IsUnicode(false);

                entity.Property(e => e.DsSigcltuser).IsUnicode(false);

                entity.Property(e => e.DsTxtHiv).IsUnicode(false);

                entity.Property(e => e.DsUpdatestaff).IsUnicode(false);

                //entity.Property(e => e.UpsizeTs)
                //    .IsRowVersion()
                //    .IsConcurrencyToken();
            });
            modelBuilder.Entity<TblDartsSrv_2017>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.DsId })
                    .HasName("PK_DartsSrv_2017")
                    .IsClustered(false);

                entity.HasIndex(e => new { e.SiteCode, e.Dsbilled, e.DsUpdate, e.DsDtAdded, e.DsDtStart, e.DsDtEnd })
                    .HasName("idx_DartsSrv_Update_2017");

                entity.Property(e => e.DsArea).IsUnicode(false);

                entity.Property(e => e.DsDbnotes).IsUnicode(false);

                entity.Property(e => e.DsDiag).IsUnicode(false);

                entity.Property(e => e.DsDiag10).IsUnicode(false);

                entity.Property(e => e.DsError).IsUnicode(false);

                entity.Property(e => e.DsSigUser).IsUnicode(false);

                entity.Property(e => e.DsSigUserCosign).IsUnicode(false);

                entity.Property(e => e.DsSigcltuser).IsUnicode(false);

                entity.Property(e => e.DsTxtHiv).IsUnicode(false);

                entity.Property(e => e.DsUpdatestaff).IsUnicode(false);

                //entity.Property(e => e.UpsizeTs)
                //    .IsRowVersion()
                //    .IsConcurrencyToken();
            });
            modelBuilder.Entity<TblDartsSrv_2018>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.DsId })
                    .HasName("PK_DartsSrv_2018")
                    .IsClustered(false);

                entity.HasIndex(e => new { e.SiteCode, e.Dsbilled, e.DsUpdate, e.DsDtAdded, e.DsDtStart, e.DsDtEnd })
                    .HasName("idx_DartsSrv_Update_2018");

                entity.Property(e => e.DsArea).IsUnicode(false);

                entity.Property(e => e.DsDbnotes).IsUnicode(false);

                entity.Property(e => e.DsDiag).IsUnicode(false);

                entity.Property(e => e.DsDiag10).IsUnicode(false);

                entity.Property(e => e.DsError).IsUnicode(false);

                entity.Property(e => e.DsSigUser).IsUnicode(false);

                entity.Property(e => e.DsSigUserCosign).IsUnicode(false);

                entity.Property(e => e.DsSigcltuser).IsUnicode(false);

                entity.Property(e => e.DsTxtHiv).IsUnicode(false);

                entity.Property(e => e.DsUpdatestaff).IsUnicode(false);

                //entity.Property(e => e.UpsizeTs)
                //    .IsRowVersion()
                //    .IsConcurrencyToken();
            });
            modelBuilder.Entity<TblDartsSrv_2019>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.DsId })
                    .HasName("PK_DartsSrv_2019")
                    .IsClustered(false);

                entity.HasIndex(e => new { e.SiteCode, e.Dsbilled, e.DsUpdate, e.DsDtAdded, e.DsDtStart, e.DsDtEnd })
                    .HasName("idx_DartsSrv_Update_2019");

                entity.Property(e => e.DsArea).IsUnicode(false);

                entity.Property(e => e.DsDbnotes).IsUnicode(false);

                entity.Property(e => e.DsDiag).IsUnicode(false);

                entity.Property(e => e.DsDiag10).IsUnicode(false);

                entity.Property(e => e.DsError).IsUnicode(false);

                entity.Property(e => e.DsSigUser).IsUnicode(false);

                entity.Property(e => e.DsSigUserCosign).IsUnicode(false);

                entity.Property(e => e.DsSigcltuser).IsUnicode(false);

                entity.Property(e => e.DsTxtHiv).IsUnicode(false);

                entity.Property(e => e.DsUpdatestaff).IsUnicode(false);

                //entity.Property(e => e.UpsizeTs)
                //    .IsRowVersion()
                //    .IsConcurrencyToken();
            });
            modelBuilder.Entity<TblDartsSrv_2020>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.DsId })
                    .HasName("PK_DartsSrv_2020")
                    .IsClustered(false);

                entity.HasIndex(e => new { e.SiteCode, e.Dsbilled, e.DsUpdate, e.DsDtAdded, e.DsDtStart, e.DsDtEnd })
                    .HasName("idx_DartsSrv_Update_2020");

                entity.Property(e => e.DsArea).IsUnicode(false);

                entity.Property(e => e.DsDbnotes).IsUnicode(false);

                entity.Property(e => e.DsDiag).IsUnicode(false);

                entity.Property(e => e.DsDiag10).IsUnicode(false);

                entity.Property(e => e.DsError).IsUnicode(false);

                entity.Property(e => e.DsSigUser).IsUnicode(false);

                entity.Property(e => e.DsSigUserCosign).IsUnicode(false);

                entity.Property(e => e.DsSigcltuser).IsUnicode(false);

                entity.Property(e => e.DsTxtHiv).IsUnicode(false);

                entity.Property(e => e.DsUpdatestaff).IsUnicode(false);

                //entity.Property(e => e.UpsizeTs)
                //    .IsRowVersion()
                //    .IsConcurrencyToken();
            });
            modelBuilder.Entity<TblDartsSrv_2021>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.DsId })
                    .HasName("PK_DartsSrv_2021")
                    .IsClustered(false);

                entity.HasIndex(e => new { e.SiteCode, e.Dsbilled, e.DsUpdate, e.DsDtAdded, e.DsDtStart, e.DsDtEnd })
                    .HasName("idx_DartsSrv_Update_2021");

                entity.Property(e => e.DsArea).IsUnicode(false);

                entity.Property(e => e.DsDbnotes).IsUnicode(false);

                entity.Property(e => e.DsDiag).IsUnicode(false);

                entity.Property(e => e.DsDiag10).IsUnicode(false);

                entity.Property(e => e.DsError).IsUnicode(false);

                entity.Property(e => e.DsSigUser).IsUnicode(false);

                entity.Property(e => e.DsSigUserCosign).IsUnicode(false);

                entity.Property(e => e.DsSigcltuser).IsUnicode(false);

                entity.Property(e => e.DsTxtHiv).IsUnicode(false);

                entity.Property(e => e.DsUpdatestaff).IsUnicode(false);

                //entity.Property(e => e.UpsizeTs)
                //    .IsRowVersion()
                //    .IsConcurrencyToken();
            });
            modelBuilder.Entity<TblDartsSrv_2022>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.DsId })
                    .HasName("PK_DartsSrv_2022")
                    .IsClustered(false);

                entity.HasIndex(e => new { e.SiteCode, e.Dsbilled, e.DsUpdate, e.DsDtAdded, e.DsDtStart, e.DsDtEnd })
                    .HasName("idx_DartsSrv_Update_2022");

                entity.Property(e => e.DsArea).IsUnicode(false);

                entity.Property(e => e.DsDbnotes).IsUnicode(false);

                entity.Property(e => e.DsDiag).IsUnicode(false);

                entity.Property(e => e.DsDiag10).IsUnicode(false);

                entity.Property(e => e.DsError).IsUnicode(false);

                entity.Property(e => e.DsSigUser).IsUnicode(false);

                entity.Property(e => e.DsSigUserCosign).IsUnicode(false);

                entity.Property(e => e.DsSigcltuser).IsUnicode(false);

                entity.Property(e => e.DsTxtHiv).IsUnicode(false);

                entity.Property(e => e.DsUpdatestaff).IsUnicode(false);

                //entity.Property(e => e.UpsizeTs)
                //    .IsRowVersion()
                //    .IsConcurrencyToken();
            });
            modelBuilder.Entity<TblDartsSrv_2023>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.DsId })
                    .HasName("PK_DartsSrv_2023")
                    .IsClustered(false);

                entity.HasIndex(e => new { e.SiteCode, e.Dsbilled, e.DsUpdate, e.DsDtAdded, e.DsDtStart, e.DsDtEnd })
                    .HasName("idx_DartsSrv_Update_2023");

                entity.Property(e => e.DsArea).IsUnicode(false);

                entity.Property(e => e.DsDbnotes).IsUnicode(false);

                entity.Property(e => e.DsDiag).IsUnicode(false);

                entity.Property(e => e.DsDiag10).IsUnicode(false);

                entity.Property(e => e.DsError).IsUnicode(false);

                entity.Property(e => e.DsSigUser).IsUnicode(false);

                entity.Property(e => e.DsSigUserCosign).IsUnicode(false);

                entity.Property(e => e.DsSigcltuser).IsUnicode(false);

                entity.Property(e => e.DsTxtHiv).IsUnicode(false);

                entity.Property(e => e.DsUpdatestaff).IsUnicode(false);

                //entity.Property(e => e.UpsizeTs)
                //    .IsRowVersion()
                //    .IsConcurrencyToken();
            });
            modelBuilder.Entity<TblDartsSrv_2024>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.DsId })
                    .HasName("PK_DartsSrv_2024")
                    .IsClustered(false);

                entity.HasIndex(e => new { e.SiteCode, e.Dsbilled, e.DsUpdate, e.DsDtAdded, e.DsDtStart, e.DsDtEnd })
                    .HasName("idx_DartsSrv_Update_2024");

                entity.Property(e => e.DsArea).IsUnicode(false);

                entity.Property(e => e.DsDbnotes).IsUnicode(false);

                entity.Property(e => e.DsDiag).IsUnicode(false);

                entity.Property(e => e.DsDiag10).IsUnicode(false);

                entity.Property(e => e.DsError).IsUnicode(false);

                entity.Property(e => e.DsSigUser).IsUnicode(false);

                entity.Property(e => e.DsSigUserCosign).IsUnicode(false);

                entity.Property(e => e.DsSigcltuser).IsUnicode(false);

                entity.Property(e => e.DsTxtHiv).IsUnicode(false);

                entity.Property(e => e.DsUpdatestaff).IsUnicode(false);

                //entity.Property(e => e.UpsizeTs)
                //    .IsRowVersion()
                //    .IsConcurrencyToken();
            });
            modelBuilder.Entity<TblEnrollment>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.SiteCode })
                    .HasName("PK_Enrollment");

                entity.HasIndex(e => new { e.CltId, e.Program, e.DischargeDate, e.EnrollDate })
                    .HasDatabaseName("Idx_Enrollment_EnrollDate");

                entity.HasIndex(e => new { e.CltId, e.Counselor, e.Dasareason, e.Deleterecord, e.DischargeDate, e.DischargeIncome, e.DischargeReasonCode, e.DischargeReasonText, e.DischargeSubReasonCode, e.DtLastContact, e.DtLastQuery, e.EnrollDate, e.EnrollReasonCode, e.EnrollReasonText, e.EnrollSubReasonCode, e.IntakeIncome, e.LastModAt, e.Module, e.Modulenote, e.NoDartsDischarge, e.NoDartsEnroll, e.OnDemand, e.ParentEnrollId, e.Physician, e.Program, e.RepOldEnroll, e.RowChkSum, e.SiteId, e.StrArrests, e.StrBaby, e.StrBabyDf, e.StrDbnotes, e.StrEduc, e.StrEmpStat, e.StrLiving, e.StrNilf, e.StrPriFreq, e.StrPriProb, e.StrSchoolJobTraining, e.StrSecFreq, e.StrSecProb, e.StrSelfHelp, e.StrSelfHelpDet, e.StrStaff, e.StrSuppInt, e.StrTerFreq, e.StrTerProb, e.Transfer, e.SiteCode })
                    .HasName("tbl_Enrollment_Updater");

                entity.Property(e => e.Counselor).IsUnicode(false);

                entity.Property(e => e.Dasareason).IsUnicode(false);

                entity.Property(e => e.Deleterecord).IsUnicode(false);

                entity.Property(e => e.DischargeReasonCode).IsUnicode(false);

                entity.Property(e => e.DischargeReasonText).IsUnicode(false);

                entity.Property(e => e.DischargeSubReasonCode).IsUnicode(false);

                entity.Property(e => e.EnrollReasonCode).IsUnicode(false);

                entity.Property(e => e.EnrollReasonText).IsUnicode(false);

                entity.Property(e => e.EnrollSubReasonCode).IsUnicode(false);

                entity.Property(e => e.Module).IsUnicode(false);

                entity.Property(e => e.Modulenote).IsUnicode(false);

                entity.Property(e => e.Physician).IsUnicode(false);

                entity.Property(e => e.Program).IsUnicode(false);

                entity.Property(e => e.StrArrests).IsUnicode(false);

                entity.Property(e => e.StrBaby).IsUnicode(false);

                entity.Property(e => e.StrBabyDf).IsUnicode(false);

                entity.Property(e => e.StrDbnotes).IsUnicode(false);

                entity.Property(e => e.StrEduc).IsUnicode(false);

                entity.Property(e => e.StrEmpStat).IsUnicode(false);

                entity.Property(e => e.StrLiving).IsUnicode(false);

                entity.Property(e => e.StrNilf).IsUnicode(false);

                entity.Property(e => e.StrPriFreq).IsUnicode(false);

                entity.Property(e => e.StrPriProb).IsUnicode(false);

                entity.Property(e => e.StrSchoolJobTraining).IsUnicode(false);

                entity.Property(e => e.StrSecFreq).IsUnicode(false);

                entity.Property(e => e.StrSecProb).IsUnicode(false);

                entity.Property(e => e.StrSelfHelp).IsUnicode(false);

                entity.Property(e => e.StrSelfHelpDet).IsUnicode(false);

                entity.Property(e => e.StrStaff).IsUnicode(false);

                entity.Property(e => e.StrSuppInt).IsUnicode(false);

                entity.Property(e => e.StrTerFreq).IsUnicode(false);

                entity.Property(e => e.StrTerProb).IsUnicode(false);

                //entity.Property(e => e.UpsizeTs)
                //    .IsRowVersion()
                //    .IsConcurrencyToken();
            });

            modelBuilder.Entity<TblFeeSched>(entity =>
            {
                entity.HasKey(e => new { e.FsId, e.FsSite })
                    .HasName("PK_FeeSched");

                entity.Property(e => e.FsId).ValueGeneratedNever();

                entity.Property(e => e.Cptcode).IsUnicode(false);

                entity.Property(e => e.DsService).IsUnicode(false);

                entity.Property(e => e.FsPayid).IsUnicode(false);

                entity.Property(e => e.Modifier).IsUnicode(false);

                entity.Property(e => e.Notes1).IsUnicode(false);

                entity.Property(e => e.Notes2).IsUnicode(false);

                entity.Property(e => e.Pos).IsUnicode(false);

                entity.Property(e => e.RevCode).IsUnicode(false);

                entity.Property(e => e.UnitMin).IsUnicode(false);
            });

            modelBuilder.Entity<TblGlobalPayor>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.PayId })
                    .HasName("PK_tblPAYER");

                entity.Property(e => e.PayId).ValueGeneratedNever();

                entity.Property(e => e.AltTaxId).IsUnicode(false);

                entity.Property(e => e.PayAddress).IsUnicode(false);

                entity.Property(e => e.PayAuthformat).IsUnicode(false);

                entity.Property(e => e.PayCity).IsUnicode(false);

                entity.Property(e => e.PayDefaultsubmit).IsUnicode(false);

                entity.Property(e => e.PayDosetype).IsUnicode(false);

                entity.Property(e => e.PayFx).IsUnicode(false);

                entity.Property(e => e.PayGlclass).IsUnicode(false);

                entity.Property(e => e.PayIndfreq).IsUnicode(false);

                entity.Property(e => e.PayLabelName).IsUnicode(false);

                entity.Property(e => e.PayName).IsUnicode(false);

                entity.Property(e => e.PayNote).IsUnicode(false);

                entity.Property(e => e.PayOverride).IsUnicode(false);

                entity.Property(e => e.PayPh).IsUnicode(false);

                entity.Property(e => e.PayPos).IsUnicode(false);

                entity.Property(e => e.PaySt).IsUnicode(false);

                entity.Property(e => e.PaySubmitType).IsUnicode(false);

                entity.Property(e => e.Payaddressjoin).IsUnicode(false);

                entity.Property(e => e.Payclass).IsUnicode(false);

                entity.Property(e => e.PayerNumber).IsUnicode(false);

                entity.Property(e => e.Paynamejoin).IsUnicode(false);

                entity.Property(e => e.Payregion).IsUnicode(false);

                entity.Property(e => e.Payzip).IsUnicode(false);

                entity.Property(e => e.Revcode).IsUnicode(false);
            });

            modelBuilder.Entity<TblGlobalDevices>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.DId });

                entity.ToTable("tbl_GlobalDevices", "ctrl");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.DId).HasColumnName("dID");

                entity.Property(e => e.DBacqueuePc).HasColumnName("dBACqueuePC");

                entity.Property(e => e.DCheckin).HasColumnName("dCHECKIN");

                entity.Property(e => e.DDeviceid)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("dDEVICEID");

                entity.Property(e => e.DDispense).HasColumnName("dDispense");

                entity.Property(e => e.DFingerprint).HasColumnName("dFingerprint");

                entity.Property(e => e.DLabel)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("dLABEL");

                entity.Property(e => e.DPumpnum).HasColumnName("dPUMPNUM");

                entity.Property(e => e.DPumptype)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("dPUMPTYPE");

                entity.Property(e => e.DReceipt)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("dRECEIPT");

                entity.Property(e => e.DSid).HasColumnName("dSID");

                entity.Property(e => e.DSigpad).HasColumnName("dSIGPAD");

                entity.Property(e => e.DTestmode).HasColumnName("dTESTMODE");

                entity.Property(e => e.DTouchScreen).HasColumnName("dTouchScreen");
            });

            modelBuilder.Entity<TblLocationCmds>(entity =>
            {
                entity.HasKey(e => e.CmdId)
                    .HasName("PK_LocationCmds");

                entity.Property(e => e.CmdId).ValueGeneratedNever();

                entity.Property(e => e.CmdStr).IsUnicode(false);
            });

            modelBuilder.Entity<TblLocationCons>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.EffectiveDate, e.ActionKey })
                    .HasName("PK_LocationCons");
            });
            modelBuilder.Entity<TblLocations>(entity =>
            {
                entity.HasKey(e => e.SiteCode)
                    .HasName("PK_Locations");

                entity.ToTable("tbl_Locations", "ctrl");

                entity.Property(e => e.SiteCode).HasMaxLength(25);

                entity.Property(e => e.AcctCmpyId)
                    .HasColumnName("AcctCmpyID")
                    .HasMaxLength(255);

                entity.Property(e => e.AltRegion).HasMaxLength(255);

                entity.Property(e => e.ClinicName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.ContractDate).HasColumnType("date");

                entity.Property(e => e.EnrollCutoff).HasColumnType("date");

                entity.Property(e => e.Latitude).HasColumnType("decimal(12, 6)");

                entity.Property(e => e.Location).HasMaxLength(255);

                entity.Property(e => e.Longitude).HasColumnType("decimal(12, 6)");

                entity.Property(e => e.RegionCode)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.SId).HasColumnName("sID");

                entity.Property(e => e.SammstrxDate)
                    .HasColumnName("SAMMSTrxDate")
                    .HasColumnType("date");

                entity.Property(e => e.SiteClinic).HasMaxLength(255);

                entity.Property(e => e.StateCode).HasMaxLength(255);

                entity.Property(e => e.VpregionCode)
                    .HasColumnName("VPRegionCode")
                    .HasMaxLength(255);

                entity.Property(e => e.ZipCode).HasMaxLength(10);
            });

            modelBuilder.Entity<TblMapSrc2Dsn>(entity =>
            {
                entity.HasKey(e => new { e.ActionKey, e.ActionStepKey, e.FieldKey })
                    .HasName("PK_MapSrc2Dsn");
            });

            modelBuilder.Entity<TblOrders>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.OrderNum, e.CltId })
                    .HasName("PK_Orders");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.ActByUser).IsUnicode(false);

                entity.Property(e => e.CltM4id).IsUnicode(false);

                entity.Property(e => e.Color).IsUnicode(false);

                entity.Property(e => e.DeActbyUser).IsUnicode(false);

                entity.Property(e => e.Doctor).IsUnicode(false);

                entity.Property(e => e.MedType).IsUnicode(false);

                entity.Property(e => e.Notes).IsUnicode(false);

                entity.Property(e => e.OUser).IsUnicode(false);

                entity.Property(e => e.OrderTypev5).IsUnicode(false);

                entity.Property(e => e.OverApprove).IsUnicode(false);

                entity.Property(e => e.OverapproveDt).IsUnicode(false);

                entity.Property(e => e.Pckcode).IsUnicode(false);

                entity.Property(e => e.RxhistId).IsUnicode(false);

                entity.Property(e => e.Stype).IsUnicode(false);

                entity.Property(e => e.Type).IsUnicode(false);
            });
            modelBuilder.Entity<TblOrders2028>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.OrderNum, e.CltId })
                    .HasName("PK_Orders_2028");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.ActByUser).IsUnicode(false);

                entity.Property(e => e.CltM4id).IsUnicode(false);

                entity.Property(e => e.Color).IsUnicode(false);

                entity.Property(e => e.DeActbyUser).IsUnicode(false);

                entity.Property(e => e.Doctor).IsUnicode(false);

                entity.Property(e => e.MedType).IsUnicode(false);

                entity.Property(e => e.Notes).IsUnicode(false);

                entity.Property(e => e.OUser).IsUnicode(false);

                entity.Property(e => e.OrderTypev5).IsUnicode(false);

                entity.Property(e => e.OverApprove).IsUnicode(false);

                entity.Property(e => e.OverapproveDt).IsUnicode(false);

                entity.Property(e => e.Pckcode).IsUnicode(false);

                entity.Property(e => e.RxhistId).IsUnicode(false);

                entity.Property(e => e.Stype).IsUnicode(false);

                entity.Property(e => e.Type).IsUnicode(false);
            });
            modelBuilder.Entity<TblOrders2027>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.OrderNum, e.CltId })
                    .HasName("PK_Orders_2027");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.ActByUser).IsUnicode(false);

                entity.Property(e => e.CltM4id).IsUnicode(false);

                entity.Property(e => e.Color).IsUnicode(false);

                entity.Property(e => e.DeActbyUser).IsUnicode(false);

                entity.Property(e => e.Doctor).IsUnicode(false);

                entity.Property(e => e.MedType).IsUnicode(false);

                entity.Property(e => e.Notes).IsUnicode(false);

                entity.Property(e => e.OUser).IsUnicode(false);

                entity.Property(e => e.OrderTypev5).IsUnicode(false);

                entity.Property(e => e.OverApprove).IsUnicode(false);

                entity.Property(e => e.OverapproveDt).IsUnicode(false);

                entity.Property(e => e.Pckcode).IsUnicode(false);

                entity.Property(e => e.RxhistId).IsUnicode(false);

                entity.Property(e => e.Stype).IsUnicode(false);

                entity.Property(e => e.Type).IsUnicode(false);
            });
            modelBuilder.Entity<TblOrders2026>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.OrderNum, e.CltId })
                    .HasName("PK_Orders_2026");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.ActByUser).IsUnicode(false);

                entity.Property(e => e.CltM4id).IsUnicode(false);

                entity.Property(e => e.Color).IsUnicode(false);

                entity.Property(e => e.DeActbyUser).IsUnicode(false);

                entity.Property(e => e.Doctor).IsUnicode(false);

                entity.Property(e => e.MedType).IsUnicode(false);

                entity.Property(e => e.Notes).IsUnicode(false);

                entity.Property(e => e.OUser).IsUnicode(false);

                entity.Property(e => e.OrderTypev5).IsUnicode(false);

                entity.Property(e => e.OverApprove).IsUnicode(false);

                entity.Property(e => e.OverapproveDt).IsUnicode(false);

                entity.Property(e => e.Pckcode).IsUnicode(false);

                entity.Property(e => e.RxhistId).IsUnicode(false);

                entity.Property(e => e.Stype).IsUnicode(false);

                entity.Property(e => e.Type).IsUnicode(false);
            });
            modelBuilder.Entity<TblOrders2025>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.OrderNum, e.CltId })
                    .HasName("PK_Orders_2025");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.ActByUser).IsUnicode(false);

                entity.Property(e => e.CltM4id).IsUnicode(false);

                entity.Property(e => e.Color).IsUnicode(false);

                entity.Property(e => e.DeActbyUser).IsUnicode(false);

                entity.Property(e => e.Doctor).IsUnicode(false);

                entity.Property(e => e.MedType).IsUnicode(false);

                entity.Property(e => e.Notes).IsUnicode(false);

                entity.Property(e => e.OUser).IsUnicode(false);

                entity.Property(e => e.OrderTypev5).IsUnicode(false);

                entity.Property(e => e.OverApprove).IsUnicode(false);

                entity.Property(e => e.OverapproveDt).IsUnicode(false);

                entity.Property(e => e.Pckcode).IsUnicode(false);

                entity.Property(e => e.RxhistId).IsUnicode(false);

                entity.Property(e => e.Stype).IsUnicode(false);

                entity.Property(e => e.Type).IsUnicode(false);
            });
            modelBuilder.Entity<TblOrders2024>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.OrderNum, e.CltId })
                    .HasName("PK_Orders_2024");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.ActByUser).IsUnicode(false);

                entity.Property(e => e.CltM4id).IsUnicode(false);

                entity.Property(e => e.Color).IsUnicode(false);

                entity.Property(e => e.DeActbyUser).IsUnicode(false);

                entity.Property(e => e.Doctor).IsUnicode(false);

                entity.Property(e => e.MedType).IsUnicode(false);

                entity.Property(e => e.Notes).IsUnicode(false);

                entity.Property(e => e.OUser).IsUnicode(false);

                entity.Property(e => e.OrderTypev5).IsUnicode(false);

                entity.Property(e => e.OverApprove).IsUnicode(false);

                entity.Property(e => e.OverapproveDt).IsUnicode(false);

                entity.Property(e => e.Pckcode).IsUnicode(false);

                entity.Property(e => e.RxhistId).IsUnicode(false);

                entity.Property(e => e.Stype).IsUnicode(false);

                entity.Property(e => e.Type).IsUnicode(false);
            });
            modelBuilder.Entity<TblOrders2023>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.OrderNum, e.CltId })
                    .HasName("PK_Orders_2023");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.ActByUser).IsUnicode(false);

                entity.Property(e => e.CltM4id).IsUnicode(false);

                entity.Property(e => e.Color).IsUnicode(false);

                entity.Property(e => e.DeActbyUser).IsUnicode(false);

                entity.Property(e => e.Doctor).IsUnicode(false);

                entity.Property(e => e.MedType).IsUnicode(false);

                entity.Property(e => e.Notes).IsUnicode(false);

                entity.Property(e => e.OUser).IsUnicode(false);

                entity.Property(e => e.OrderTypev5).IsUnicode(false);

                entity.Property(e => e.OverApprove).IsUnicode(false);

                entity.Property(e => e.OverapproveDt).IsUnicode(false);

                entity.Property(e => e.Pckcode).IsUnicode(false);

                entity.Property(e => e.RxhistId).IsUnicode(false);

                entity.Property(e => e.Stype).IsUnicode(false);

                entity.Property(e => e.Type).IsUnicode(false);
            });
            modelBuilder.Entity<TblOrders2022>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.OrderNum, e.CltId })
                    .HasName("PK_Orders_2022");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.ActByUser).IsUnicode(false);

                entity.Property(e => e.CltM4id).IsUnicode(false);

                entity.Property(e => e.Color).IsUnicode(false);

                entity.Property(e => e.DeActbyUser).IsUnicode(false);

                entity.Property(e => e.Doctor).IsUnicode(false);

                entity.Property(e => e.MedType).IsUnicode(false);

                entity.Property(e => e.Notes).IsUnicode(false);

                entity.Property(e => e.OUser).IsUnicode(false);

                entity.Property(e => e.OrderTypev5).IsUnicode(false);

                entity.Property(e => e.OverApprove).IsUnicode(false);

                entity.Property(e => e.OverapproveDt).IsUnicode(false);

                entity.Property(e => e.Pckcode).IsUnicode(false);

                entity.Property(e => e.RxhistId).IsUnicode(false);

                entity.Property(e => e.Stype).IsUnicode(false);

                entity.Property(e => e.Type).IsUnicode(false);
            });
            modelBuilder.Entity<TblOrders2021>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.OrderNum, e.CltId })
                    .HasName("PK_Orders_2021");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.ActByUser).IsUnicode(false);

                entity.Property(e => e.CltM4id).IsUnicode(false);

                entity.Property(e => e.Color).IsUnicode(false);

                entity.Property(e => e.DeActbyUser).IsUnicode(false);

                entity.Property(e => e.Doctor).IsUnicode(false);

                entity.Property(e => e.MedType).IsUnicode(false);

                entity.Property(e => e.Notes).IsUnicode(false);

                entity.Property(e => e.OUser).IsUnicode(false);

                entity.Property(e => e.OrderTypev5).IsUnicode(false);

                entity.Property(e => e.OverApprove).IsUnicode(false);

                entity.Property(e => e.OverapproveDt).IsUnicode(false);

                entity.Property(e => e.Pckcode).IsUnicode(false);

                entity.Property(e => e.RxhistId).IsUnicode(false);

                entity.Property(e => e.Stype).IsUnicode(false);

                entity.Property(e => e.Type).IsUnicode(false);
            });
            modelBuilder.Entity<TblOrders2020>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.OrderNum, e.CltId })
                    .HasName("PK_Orders_2020");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.ActByUser).IsUnicode(false);

                entity.Property(e => e.CltM4id).IsUnicode(false);

                entity.Property(e => e.Color).IsUnicode(false);

                entity.Property(e => e.DeActbyUser).IsUnicode(false);

                entity.Property(e => e.Doctor).IsUnicode(false);

                entity.Property(e => e.MedType).IsUnicode(false);

                entity.Property(e => e.Notes).IsUnicode(false);

                entity.Property(e => e.OUser).IsUnicode(false);

                entity.Property(e => e.OrderTypev5).IsUnicode(false);

                entity.Property(e => e.OverApprove).IsUnicode(false);

                entity.Property(e => e.OverapproveDt).IsUnicode(false);

                entity.Property(e => e.Pckcode).IsUnicode(false);

                entity.Property(e => e.RxhistId).IsUnicode(false);

                entity.Property(e => e.Stype).IsUnicode(false);

                entity.Property(e => e.Type).IsUnicode(false);
            });
            modelBuilder.Entity<TblOrders2019>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.OrderNum, e.CltId })
                    .HasName("PK_Orders_2019");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.ActByUser).IsUnicode(false);

                entity.Property(e => e.CltM4id).IsUnicode(false);

                entity.Property(e => e.Color).IsUnicode(false);

                entity.Property(e => e.DeActbyUser).IsUnicode(false);

                entity.Property(e => e.Doctor).IsUnicode(false);

                entity.Property(e => e.MedType).IsUnicode(false);

                entity.Property(e => e.Notes).IsUnicode(false);

                entity.Property(e => e.OUser).IsUnicode(false);

                entity.Property(e => e.OrderTypev5).IsUnicode(false);

                entity.Property(e => e.OverApprove).IsUnicode(false);

                entity.Property(e => e.OverapproveDt).IsUnicode(false);

                entity.Property(e => e.Pckcode).IsUnicode(false);

                entity.Property(e => e.RxhistId).IsUnicode(false);

                entity.Property(e => e.Stype).IsUnicode(false);

                entity.Property(e => e.Type).IsUnicode(false);
            });
            modelBuilder.Entity<TblOrders2018>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.OrderNum, e.CltId })
                    .HasName("PK_Orders_2018");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.ActByUser).IsUnicode(false);

                entity.Property(e => e.CltM4id).IsUnicode(false);

                entity.Property(e => e.Color).IsUnicode(false);

                entity.Property(e => e.DeActbyUser).IsUnicode(false);

                entity.Property(e => e.Doctor).IsUnicode(false);

                entity.Property(e => e.MedType).IsUnicode(false);

                entity.Property(e => e.Notes).IsUnicode(false);

                entity.Property(e => e.OUser).IsUnicode(false);

                entity.Property(e => e.OrderTypev5).IsUnicode(false);

                entity.Property(e => e.OverApprove).IsUnicode(false);

                entity.Property(e => e.OverapproveDt).IsUnicode(false);

                entity.Property(e => e.Pckcode).IsUnicode(false);

                entity.Property(e => e.RxhistId).IsUnicode(false);

                entity.Property(e => e.Stype).IsUnicode(false);

                entity.Property(e => e.Type).IsUnicode(false);
            });
            modelBuilder.Entity<TblOrders2017>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.OrderNum, e.CltId })
                    .HasName("PK_Orders_2017");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.ActByUser).IsUnicode(false);

                entity.Property(e => e.CltM4id).IsUnicode(false);

                entity.Property(e => e.Color).IsUnicode(false);

                entity.Property(e => e.DeActbyUser).IsUnicode(false);

                entity.Property(e => e.Doctor).IsUnicode(false);

                entity.Property(e => e.MedType).IsUnicode(false);

                entity.Property(e => e.Notes).IsUnicode(false);

                entity.Property(e => e.OUser).IsUnicode(false);

                entity.Property(e => e.OrderTypev5).IsUnicode(false);

                entity.Property(e => e.OverApprove).IsUnicode(false);

                entity.Property(e => e.OverapproveDt).IsUnicode(false);

                entity.Property(e => e.Pckcode).IsUnicode(false);

                entity.Property(e => e.RxhistId).IsUnicode(false);

                entity.Property(e => e.Stype).IsUnicode(false);

                entity.Property(e => e.Type).IsUnicode(false);
            });
            modelBuilder.Entity<TblOrders2016>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.OrderNum, e.CltId })
                    .HasName("PK_Orders_2016");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.ActByUser).IsUnicode(false);

                entity.Property(e => e.CltM4id).IsUnicode(false);

                entity.Property(e => e.Color).IsUnicode(false);

                entity.Property(e => e.DeActbyUser).IsUnicode(false);

                entity.Property(e => e.Doctor).IsUnicode(false);

                entity.Property(e => e.MedType).IsUnicode(false);

                entity.Property(e => e.Notes).IsUnicode(false);

                entity.Property(e => e.OUser).IsUnicode(false);

                entity.Property(e => e.OrderTypev5).IsUnicode(false);

                entity.Property(e => e.OverApprove).IsUnicode(false);

                entity.Property(e => e.OverapproveDt).IsUnicode(false);

                entity.Property(e => e.Pckcode).IsUnicode(false);

                entity.Property(e => e.RxhistId).IsUnicode(false);

                entity.Property(e => e.Stype).IsUnicode(false);

                entity.Property(e => e.Type).IsUnicode(false);
            });
            modelBuilder.Entity<VwOrders>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("vw_Orders", "pats");

                entity.Property(e => e.ActByUser)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.ActbyDate)
                    .HasColumnType("datetime")
                    .HasColumnName("ActbyDATE");

                entity.Property(e => e.Aws).HasColumnName("aws");

                entity.Property(e => e.BlSched).HasColumnName("blSched");

                entity.Property(e => e.BlVerbal).HasColumnName("blVerbal");

                entity.Property(e => e.Blind).HasColumnName("BLIND");

                entity.Property(e => e.CltId).HasColumnName("cltID");

                entity.Property(e => e.CltM4id)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("cltM4ID");

                entity.Property(e => e.Color)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.DateAdded).HasColumnType("datetime");

                entity.Property(e => e.DeActbyDate).HasColumnType("datetime");

                entity.Property(e => e.DeActbyUser)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.Doctor)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Dose).HasColumnType("decimal(8, 3)");

                entity.Property(e => e.Dose2).HasColumnType("decimal(8, 3)");

                entity.Property(e => e.DtSig)
                    .HasColumnType("datetime")
                    .HasColumnName("dtSig");

                entity.Property(e => e.Dtmid)
                    .HasColumnType("datetime")
                    .HasColumnName("DTMID");

                entity.Property(e => e.EffectiveDate).HasColumnType("datetime");

                entity.Property(e => e.Ex).HasColumnName("EX");

                entity.Property(e => e.ExpirationDate).HasColumnType("datetime");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.MedType)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("medType");

                entity.Property(e => e.Newdose).HasColumnName("newdose");

                entity.Property(e => e.Notes)
                    .HasMaxLength(1000)
                    .IsUnicode(false);

                entity.Property(e => e.OUser)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("o_User");

                entity.Property(e => e.OrderTypev5)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Orderdate).HasColumnType("datetime");

                entity.Property(e => e.OverApprove)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.OverapproveDt)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("OverapproveDT");

                entity.Property(e => e.Pckcode)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("pckcode");

                entity.Property(e => e.RepOldOrder)
                    .HasColumnType("numeric(18, 0)")
                    .HasColumnName("repOldOrder");

                entity.Property(e => e.RxhistId)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("rxhistID");

                entity.Property(e => e.SigDr)
                    .HasColumnType("ntext")
                    .HasColumnName("sigDr");

                entity.Property(e => e.SigDrImg).HasColumnName("sigDrImg");

                entity.Property(e => e.SigMid)
                    .HasColumnType("ntext")
                    .HasColumnName("sigMID");

                entity.Property(e => e.SigMidImg).HasColumnName("sigMidImg");

                entity.Property(e => e.SigNotedImg).HasColumnName("sigNotedImg");

                entity.Property(e => e.SigNoteddt)
                    .HasColumnType("datetime")
                    .HasColumnName("sigNOTEDDT");

                entity.Property(e => e.Sigentered)
                    .HasColumnType("ntext")
                    .HasColumnName("sigentered");

                entity.Property(e => e.Sigentereddt)
                    .HasColumnType("datetime")
                    .HasColumnName("sigentereddt");

                entity.Property(e => e.Signoted)
                    .HasColumnType("ntext")
                    .HasColumnName("signoted");

                entity.Property(e => e.SiteCode)
                    .IsRequired()
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.SplitFirst).HasColumnName("splitFIRST");

                entity.Property(e => e.Stype)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Type)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.White).HasColumnName("white");
            });

            modelBuilder.Entity<TblPayerClient>(entity =>
            {
                entity.HasKey(e => e.Pcid)
                    .HasName("PK_PayerClient");

                entity.ToTable("tbl_PayerClient", "pats");

                entity.Property(e => e.Pcid).HasColumnName("PCID");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.PyActive).HasColumnName("pyACTIVE");

                entity.Property(e => e.PyAddDate)
                    .HasColumnType("date")
                    .HasColumnName("pyAddDate");

                entity.Property(e => e.PyAddUser)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.PyAuth)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("pyAUTH");

                entity.Property(e => e.PyBack)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("pyBACK");

                entity.Property(e => e.PyBasicNum)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("pyBasicNum");

                entity.Property(e => e.PyCategory)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("pyCategory");

                entity.Property(e => e.PyCltid).HasColumnName("pyCLTID");

                entity.Property(e => e.PyDbnotes)
                    .HasMaxLength(250)
                    .IsUnicode(false)
                    .HasColumnName("pyDBnotes");

                entity.Property(e => e.PyDob)
                    .HasColumnType("datetime")
                    .HasColumnName("pyDOB");

                entity.Property(e => e.PyEligCheck)
                    .HasColumnType("datetime")
                    .HasColumnName("pyEligCheck");

                entity.Property(e => e.PyEligUser)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("pyEligUser");

                entity.Property(e => e.PyEnd)
                    .HasColumnType("datetime")
                    .HasColumnName("pyEND");

                entity.Property(e => e.PyGroup)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("pyGROUP");

                entity.Property(e => e.PyHmoprovider)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("pyHMOprovider");

                entity.Property(e => e.PyId).HasColumnName("pyID");

                entity.Property(e => e.PyLocalOffice)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("pyLocalOffice");

                entity.Property(e => e.PyPayerid)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("pyPAYERID");

                entity.Property(e => e.PyPayertype)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("pyPAYERTYPE");

                entity.Property(e => e.PyPhone)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("pyPhone");

                entity.Property(e => e.PyProjectedEnd)
                    .HasColumnType("date")
                    .HasColumnName("pyProjectedEnd");

                entity.Property(e => e.PyStart)
                    .HasColumnType("datetime")
                    .HasColumnName("pySTART");

                entity.Property(e => e.PySubsid)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("pySUBSID");

                entity.Property(e => e.Pyadd)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("pyadd");

                entity.Property(e => e.Pybupe)
                    .HasMaxLength(250)
                    .IsUnicode(false)
                    .HasColumnName("pybupe");

                entity.Property(e => e.Pycity)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("pycity");

                entity.Property(e => e.Pycoins)
                    .HasColumnType("money")
                    .HasColumnName("pycoins");

                entity.Property(e => e.Pycopay)
                    .HasColumnType("money")
                    .HasColumnName("pycopay");

                entity.Property(e => e.Pyded)
                    .HasMaxLength(250)
                    .IsUnicode(false)
                    .HasColumnName("pyded");

                entity.Property(e => e.Pydeduct)
                    .HasColumnType("money")
                    .HasColumnName("pydeduct");

                entity.Property(e => e.Pydeductleft)
                    .HasColumnType("money")
                    .HasColumnName("pydeductleft");

                entity.Property(e => e.Pyfirst)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("pyfirst");

                entity.Property(e => e.Pyfront)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("pyfront");

                entity.Property(e => e.Pylast)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("pylast");

                entity.Property(e => e.Pymmt)
                    .HasMaxLength(250)
                    .IsUnicode(false)
                    .HasColumnName("pymmt");

                entity.Property(e => e.Pyout)
                    .HasMaxLength(250)
                    .IsUnicode(false)
                    .HasColumnName("pyout");

                entity.Property(e => e.Pysame).HasColumnName("pysame");

                entity.Property(e => e.Pystate)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("pystate");

                entity.Property(e => e.Pyzip)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("pyzip");

                entity.Property(e => e.SiteCode)
                    .IsRequired()
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.TempSavePayer)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("tempSavePayer");

                entity.Property(e => e.TypeOfAgreementCode)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });
            modelBuilder.Entity<TblPayerCltHistory>(entity =>
            {
                entity.HasKey(e => e.PchId)
                    .HasName("PK_PayerCltHistory");

                entity.ToTable("tbl_PayerCltHistory", "pats");
            });

            modelBuilder.Entity<TblSchedule>(entity =>
            {
                entity.HasKey(e => e.ScheduleId)
                    .HasName("PK_Schedule");

                entity.Property(e => e.ScheduleId).ValueGeneratedNever();

                entity.Property(e => e.Name).IsUnicode(false);
            });

            modelBuilder.Entity<TblTasks>(entity =>
            {
                entity.HasKey(e => e.TaskId)
                    .HasName("PK_Tasks");

                entity.Property(e => e.Duration).IsUnicode(false);

                entity.Property(e => e.LastModBy).IsUnicode(false);

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.TaskName).IsUnicode(false);
            });

            modelBuilder.Entity<TblTriggers>(entity =>
            {
                entity.HasKey(e => e.TriggerKey)
                    .HasName("PK_Triggers");
            });

            modelBuilder.Entity<TblUaresultDetail>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.UardId, e.UardRecId })
                    .HasName("PK_UAResultDetail");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.UaDetail).IsUnicode(false);

                entity.Property(e => e.UardFullNote).IsUnicode(false);

                entity.Property(e => e.UardKey).IsUnicode(false);

                entity.Property(e => e.UardNote).IsUnicode(false);

                entity.Property(e => e.UardResult).IsUnicode(false);
            });

            modelBuilder.Entity<TblUaresults>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.UarId }).HasName("PK_UAResults");

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.Location).IsUnicode(false);

                entity.Property(e => e.OldClient).IsUnicode(false);

                entity.Property(e => e.UaBase64).IsUnicode(false);

                entity.Property(e => e.UaDbnotes).IsUnicode(false);

                entity.Property(e => e.UaNurseNote).IsUnicode(false);

                entity.Property(e => e.UaSigUser).IsUnicode(false);

                entity.Property(e => e.UaType).IsUnicode(false);

                entity.Property(e => e.Uaprogram).IsUnicode(false);

                entity.Property(e => e.UarCreatedBy).IsUnicode(false);

                entity.Property(e => e.UarLabKey).IsUnicode(false);

                entity.Property(e => e.UarUpdatedBy).IsUnicode(false);

                //entity.Property(e => e.UpsizeTs)
                //    .IsRowVersion()
                //    .IsConcurrencyToken();
            });

            modelBuilder.Entity<TblUasched>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.UasId, e.UasLngCltId })
                    .HasName("PK_UASched");

                entity.ToTable("tbl_UASched", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.UasId).HasColumnName("uasID");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.LngCpano).HasColumnName("lngCPAno");

                entity.Property(e => e.OldClient)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("oldClient");

                entity.Property(e => e.OldNum)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("oldNum");

                entity.Property(e => e.RepOldUas)
                    .HasColumnType("numeric(18, 0)")
                    .HasColumnName("repOldUAs");

                entity.Property(e => e.Uapriority)
                    .HasMaxLength(15)
                    .IsUnicode(false)
                    .HasColumnName("uapriority");

                entity.Property(e => e.UasCollectedBy)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("uasCollectedBy");

                entity.Property(e => e.UasCollectedDate)
                    .HasColumnType("datetime")
                    .HasColumnName("uasCollectedDate");

                entity.Property(e => e.UasDt)
                    .HasColumnType("datetime")
                    .HasColumnName("uasDt");

                entity.Property(e => e.UasDtAdded)
                    .HasColumnType("datetime")
                    .HasColumnName("uasDtAdded");

                entity.Property(e => e.UasEtg).HasColumnName("uasETG");

                entity.Property(e => e.UasLngCltId).HasColumnName("uasLngCltID");

                entity.Property(e => e.UasManifestDate)
                    .HasColumnType("datetime")
                    .HasColumnName("uasManifestDate");

                entity.Property(e => e.UasNote)
                    .HasMaxLength(800)
                    .IsUnicode(false)
                    .HasColumnName("uasNOTE");

                entity.Property(e => e.UasPanel)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("uasPanel");

                entity.Property(e => e.UasPanelOther)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("uasPanelOther");

                entity.Property(e => e.UasStat)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("uasStat");

                entity.Property(e => e.UasStatDt)
                    .HasColumnType("datetime")
                    .HasColumnName("uasStatDt");

                entity.Property(e => e.UasStatUser)
                    .HasMaxLength(50)
                    .HasColumnName("uasStatUser");

                entity.Property(e => e.UasType)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("uasType");

                entity.Property(e => e.Uasticketprintdate)
                    .HasColumnType("datetime")
                    .HasColumnName("uasticketprintdate");
            });

            modelBuilder.Entity<TblUser>(entity =>
            {
                entity.Property(e => e.Uskey).ValueGeneratedNever();

                entity.Property(e => e.UsrDea).IsUnicode(false);

                entity.Property(e => e.UsrDescription).IsUnicode(false);

                entity.Property(e => e.UsrExt).IsUnicode(false);

                entity.Property(e => e.UsrFname).IsUnicode(false);

                entity.Property(e => e.UsrLname).IsUnicode(false);

                entity.Property(e => e.UsrLocation).IsUnicode(false);

                entity.Property(e => e.UsrName).IsUnicode(false);

                entity.Property(e => e.UsrPassword).IsUnicode(false);

                entity.Property(e => e.UsrPin).IsUnicode(false);

                entity.Property(e => e.UsrRole).IsUnicode(false);

                entity.Property(e => e.UsrSsn).IsUnicode(false);

                entity.Property(e => e.UsrSuper).IsUnicode(false);

                entity.Property(e => e.UsrTemplate).IsUnicode(false);

                entity.Property(e => e.Usrcred).IsUnicode(false);

                entity.Property(e => e.Usrnpi).IsUnicode(false);

                entity.Property(e => e.Usrphone).IsUnicode(false);

                entity.Property(e => e.Usrxdea).IsUnicode(false);
            });
            modelBuilder.Entity<TblUserSites>(entity =>
            {
                entity.Property(e => e.UsId).ValueGeneratedNever();

                entity.Property(e => e.UsName).IsUnicode(false);
            });

            modelBuilder.Entity<TblXref>(entity =>
            {
                entity.HasKey(e => e.Xref)
                    .HasName("Pk_XRef");

                entity.Property(e => e.Code).IsUnicode(false);

                entity.Property(e => e.Descrptn).IsUnicode(false);
            });

            modelBuilder.Entity<VwMapAction>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("vw_MapAction", "dms");

                entity.Property(e => e.ConName).IsUnicode(false);

                entity.Property(e => e.ConType).IsUnicode(false);

                entity.Property(e => e.CtrlMethod).IsUnicode(false);

                entity.Property(e => e.DsnSchema).IsUnicode(false);

                entity.Property(e => e.DsnTbl).IsUnicode(false);

                entity.Property(e => e.FromTblVw).IsUnicode(false);

                entity.Property(e => e.SortOrder).IsUnicode(false);

                entity.Property(e => e.SrcSchema).IsUnicode(false);

                entity.Property(e => e.WhereCondition).IsUnicode(false);
            });

            modelBuilder.Entity<VwMapSrc2Dsn>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("vw_MapSrc2Dsn", "dms");

                entity.Property(e => e.CompKey).IsUnicode(false);
            });

            modelBuilder.Entity<VwReinitRunSchd>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("vw_ReinitRunSchd", "tsk");

                entity.Property(e => e.ConName).IsUnicode(false);

                entity.Property(e => e.ConType).IsUnicode(false);

                entity.Property(e => e.CtrlMethod).IsUnicode(false);

                entity.Property(e => e.DsnSchema).IsUnicode(false);

                entity.Property(e => e.DsnTbl).IsUnicode(false);

                entity.Property(e => e.FromTblVw).IsUnicode(false);

                entity.Property(e => e.SiteCode).IsUnicode(false);

                entity.Property(e => e.SortOrder).IsUnicode(false);

                entity.Property(e => e.SrcSchema).IsUnicode(false);

                entity.Property(e => e.WhereCondition).IsUnicode(false);
            });

            modelBuilder.Entity<VwXref>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("vw_XRef", "ctrl");

                entity.Property(e => e.Code).IsUnicode(false);

                entity.Property(e => e.CodeType).IsUnicode(false);

                entity.Property(e => e.Description).IsUnicode(false);
            });

            modelBuilder.Entity<TblFormsSammsclient>(entity =>
            {
                entity.HasKey(e => e.Fscsid)
                    .HasName("PK_tblFORMSSAMMSCLIENT");

                entity.ToTable("tbl_FormsSAMMSClient", "pats");

                entity.Property(e => e.Fscsid)
                    .ValueGeneratedNever()
                    .HasColumnName("fscsid");

                entity.Property(e => e.AdminNurseSig).HasColumnType("ntext");

                entity.Property(e => e.AdminNurseSigBy)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.AdminnurseSigDate).HasColumnType("date");

                entity.Property(e => e.Bac).HasColumnName("BAC");

                entity.Property(e => e.ClientSig)
                    .HasColumnType("ntext")
                    .HasColumnName("clientSig");

                entity.Property(e => e.ClientSigDate)
                    .HasColumnType("date")
                    .HasColumnName("clientSigDate");

                entity.Property(e => e.ClientSigImg).HasColumnName("clientSigImg");

                entity.Property(e => e.Doctext)
                    .IsUnicode(false)
                    .HasColumnName("doctext");

                entity.Property(e => e.DoctextEditBy)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("doctextEditBy");

                entity.Property(e => e.DoctextEditDate)
                    .HasColumnType("datetime")
                    .HasColumnName("doctextEditDate");

                entity.Property(e => e.FscCltid).HasColumnName("fscCLTID");

                entity.Property(e => e.FscDate)
                    .HasColumnType("date")
                    .HasColumnName("fscDATE");

                entity.Property(e => e.FscFormid)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("fscFORMID");

                entity.Property(e => e.Fscform)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("fscform");

                entity.Property(e => e.Fscsite).HasColumnName("fscsite");

                entity.Property(e => e.GuardianSig).HasColumnType("ntext");

                entity.Property(e => e.GuardianSigBy)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.GuardianSigDate).HasColumnType("date");

                entity.Property(e => e.NurseSig)
                    .HasColumnType("ntext")
                    .HasColumnName("nurseSig");

                entity.Property(e => e.NurseSigBy)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("nurseSigBy");

                entity.Property(e => e.NurseSigDate)
                    .HasColumnType("date")
                    .HasColumnName("nurseSigDate");

                entity.Property(e => e.NurseSigImg).HasColumnName("nurseSigImg");

                entity.Property(e => e.PhysicianSig)
                    .HasColumnType("ntext")
                    .HasColumnName("physicianSig");

                entity.Property(e => e.PhysicianSigBy)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("physicianSigBy");

                entity.Property(e => e.PhysicianSigDate)
                    .HasColumnType("date")
                    .HasColumnName("physicianSigDate");

                entity.Property(e => e.PhysicianSigImg).HasColumnName("physicianSigImg");

                entity.Property(e => e.StaffSig)
                    .HasColumnType("ntext")
                    .HasColumnName("staffSig");

                entity.Property(e => e.StaffSigBy)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("staffSigBy");

                entity.Property(e => e.StaffSigDate)
                    .HasColumnType("date")
                    .HasColumnName("staffSigDate");

                entity.Property(e => e.StaffSigImg).HasColumnName("staffSigImg");

                entity.Property(e => e.SupervisorSig)
                    .HasColumnType("ntext")
                    .HasColumnName("supervisorSig");

                entity.Property(e => e.SupervisorSigBy)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("supervisorSigBy");

                entity.Property(e => e.SupervisorSigDate)
                    .HasColumnType("date")
                    .HasColumnName("supervisorSigDate");

                entity.Property(e => e.SupervisorSigImg).HasColumnName("supervisorSigImg");
            });

            modelBuilder.Entity<TblRowTrax>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Rcdate, e.TblName });

                entity.ToTable("tbl_RowTrax", "tsk");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.Rcdate)
                    .HasColumnName("RCDate")
                    .HasColumnType("datetime");

                entity.Property(e => e.TblName)
                    .HasColumnName("tblName")
                    .HasMaxLength(200)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TblPreAdmissionReferralSource>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Id })
                    .HasName("PK_PreAdmissionReferralSource");

                entity.ToTable("tbl_PreAdmissionReferralSource", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .IsFixedLength();

                entity.Property(e => e.CreatedBy)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedOn).HasColumnType("datetime");

                entity.Property(e => e.Email)
                    .HasMaxLength(400)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdateOn).HasColumnType("datetime");

                entity.Property(e => e.LastUpdatedBy)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.MostWantToDoDifferently)
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.Name)
                    .HasMaxLength(400)
                    .IsUnicode(false);

                entity.Property(e => e.Organization)
                    .HasMaxLength(400)
                    .IsUnicode(false);

                entity.Property(e => e.Phone)
                    .HasMaxLength(40)
                    .IsUnicode(false);

                entity.Property(e => e.PrimaryReferralSource)
                    .HasMaxLength(35)
                    .IsUnicode(false);

                entity.Property(e => e.Program)
                    .HasMaxLength(30)
                    .IsUnicode(false)
                    .IsFixedLength();

                entity.Property(e => e.ReferralName)
                    .HasMaxLength(400)
                    .IsUnicode(false);

                entity.Property(e => e.ReferralNameId)
                    .HasMaxLength(256)
                    .IsUnicode(false)
                    .HasColumnName("ReferralNameID");

                entity.Property(e => e.ReferralOrganization)
                    .HasMaxLength(400)
                    .IsUnicode(false);

                entity.Property(e => e.ReferralOrganizationId)
                    .HasMaxLength(256)
                    .IsUnicode(false)
                    .HasColumnName("ReferralOrganizationID");

                entity.Property(e => e.ReferralSourceNote)
                    .HasMaxLength(800)
                    .IsUnicode(false);

                entity.Property(e => e.SecondaryReferralSource)
                    .HasMaxLength(65)
                    .IsUnicode(false);

                entity.Property(e => e.WhyComingBackToBhg)
                    .HasMaxLength(150)
                    .IsUnicode(false)
                    .HasColumnName("WhyComingBackToBHG");

                entity.Property(e => e.WhyLeftTreatmentOfBhg)
                    .HasMaxLength(150)
                    .IsUnicode(false)
                    .HasColumnName("WhyLeftTreatmentOfBHG");
            });
            
            modelBuilder.Entity<TblBottle>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.BottleId })
                    .HasName("PK_Tbl_Bottle")
                    .IsClustered(false);

                entity.ToTable("tbl_Bottle", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.BottleId).HasColumnName("BottleID");

                entity.Property(e => e.BlnClosed).HasColumnName("blnClosed");

                entity.Property(e => e.BottleType)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.BrId).HasColumnName("brID");

                entity.Property(e => e.Color)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Deanum)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("DEANum");

                entity.Property(e => e.DtClosed)
                    .HasColumnType("datetime")
                    .HasColumnName("dtClosed");

                entity.Property(e => e.DtReceived)
                    .HasColumnType("datetime")
                    .HasColumnName("dtReceived");

                entity.Property(e => e.ExpDate).HasColumnType("datetime");

                entity.Property(e => e.InvGroup)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("invGROUP");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.LotNumber)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Manufacturer)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.SiteId).HasColumnName("SiteID");

                entity.Property(e => e.SpecGrav).HasColumnType("decimal(18, 5)");

                entity.Property(e => e.StrUser)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("strUser");

                entity.Property(e => e.Weight).HasColumnType("decimal(18, 5)");
            });

            modelBuilder.Entity<TblInvtype>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.Invid })
                    .HasName("PK_tblINVTYPE");

                entity.ToTable("tbl_INVTYPE", "ctrl");

                entity.Property(e => e.Invid)
                    .ValueGeneratedNever()
                    .HasColumnName("INVid");

                entity.Property(e => e.DisplayName)
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.InvActual)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("invACtual");

                entity.Property(e => e.InvDivision).HasColumnName("invDIVISION");

                entity.Property(e => e.InvJcode)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("invJCODE");

                entity.Property(e => e.InvLabelName)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("invLabelName");

                entity.Property(e => e.InvLiquid).HasColumnName("invLIQUID");

                entity.Property(e => e.InvMedclass)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("invMEDCLASS");

                entity.Property(e => e.InvName)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("invNAME");

                entity.Property(e => e.InvNdc)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("invNDC");

                entity.Property(e => e.InvTotal).HasColumnName("invTOTAL");

                entity.Property(e => e.InvUnit).HasColumnName("invUNIT");

                entity.Property(e => e.IsFilm).HasColumnName("isFILM");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.SiteCode)
                    .IsRequired()
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.Type)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TblLiquidLog>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.LiqId })
                    .IsClustered(false);

                entity.ToTable("tbl_LiquidLog", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.LiqId).HasColumnName("liqID");

                entity.Property(e => e.AcknowledgeDate)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("acknowledgeDate");

                entity.Property(e => e.AcknowledgeUser)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("acknowledgeUser");

                entity.Property(e => e.Amt).HasColumnName("amt");

                entity.Property(e => e.BkrId).HasColumnName("bkrID");

                entity.Property(e => e.BlLogOnly).HasColumnName("blLogOnly");

                entity.Property(e => e.BlPrepack).HasColumnName("blPrepack");

                entity.Property(e => e.BtlId).HasColumnName("btlID");

                entity.Property(e => e.ComplainceUser)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ComplianceDate).HasColumnType("datetime");

                entity.Property(e => e.Desc)
                    .HasMaxLength(750)
                    .IsUnicode(false)
                    .HasColumnName("desc");

                entity.Property(e => e.DoseId).HasColumnName("doseID");

                entity.Property(e => e.DtRti)
                    .HasColumnType("datetime")
                    .HasColumnName("dtRTI");

                entity.Property(e => e.Dtm)
                    .HasColumnType("datetime")
                    .HasColumnName("dtm");

                entity.Property(e => e.Invgroup)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("invgroup");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.Memo)
                    .HasMaxLength(500)
                    .IsUnicode(false)
                    .HasColumnName("memo");

                entity.Property(e => e.Memonew)
                    .HasMaxLength(500)
                    .IsUnicode(false)
                    .HasColumnName("memonew");

                entity.Property(e => e.RegionalDate).HasColumnType("datetime");

                entity.Property(e => e.RegionalUser)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.SiteId).HasColumnName("SiteID");

                entity.Property(e => e.Staff)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("staff");
            });
            
            modelBuilder.Entity<TblOrientationChecklistNew>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.CheckListId });

                entity.ToTable("Tbl_OrientationChecklistNew", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.Aidshivprevention).HasColumnName("AIDSHIVPrevention");

                entity.Property(e => e.CreatedBy).HasMaxLength(100);

                entity.Property(e => e.CreatedOn).HasColumnType("datetime");

                entity.Property(e => e.LastModEtl)
                    .HasColumnType("datetime")
                    .HasColumnName("LastModETL");

                entity.Property(e => e.ModifiedBy).HasMaxLength(100);

                entity.Property(e => e.ModifiedOn).HasColumnType("datetime");

                entity.Property(e => e.PatientSignatureBy).HasMaxLength(200);

                entity.Property(e => e.PatientSignatureDate).HasColumnType("datetime");

                entity.Property(e => e.StaffSignatureBy).HasMaxLength(200);

                entity.Property(e => e.StaffSignatureDate).HasColumnType("datetime");

                entity.Property(e => e.Version).HasMaxLength(100);
            });
            modelBuilder.Entity<TblLabresult>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.LabrId, e.LablngCltId })
                    .HasName("tbl_LABRESULT_PK")
                    .IsClustered(false);

                entity.ToTable("tbl_LABRESULT", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.LabrId).HasColumnName("LABrID");

                entity.Property(e => e.LablngCltId).HasColumnName("LABLngCltID");

                entity.Property(e => e.LabBase64).HasColumnName("labBase64");

                entity.Property(e => e.LabOrderId)
                    .HasMaxLength(40)
                    .IsUnicode(false)
                    .HasColumnName("labOrderId");

                entity.Property(e => e.Labnote)
                    .HasColumnType("ntext")
                    .HasColumnName("labnote");

                entity.Property(e => e.LabrCreatedBy)
                    .HasMaxLength(50)
                    .HasColumnName("LABrCreatedBy");

                entity.Property(e => e.LabrCreatedDt)
                    .HasColumnType("datetime")
                    .HasColumnName("LABrCreatedDt");

                entity.Property(e => e.LabrDropDt)
                    .HasColumnType("datetime")
                    .HasColumnName("LABrDropDt");

                entity.Property(e => e.LabrLotno)
                    .HasMaxLength(160)
                    .HasColumnName("labrLotno");

                entity.Property(e => e.LabrResultDt)
                    .HasColumnType("datetime")
                    .HasColumnName("LABrResultDt");

                entity.Property(e => e.LabrSchedId).HasColumnName("LABrSchedID");

                entity.Property(e => e.LabrUpdatedBy)
                    .HasMaxLength(50)
                    .HasColumnName("LABrUpdatedBy");

                entity.Property(e => e.LabrUpdatedDt)
                    .HasColumnType("datetime")
                    .HasColumnName("LABrUpdatedDt");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");

                entity.Property(e => e.RepOldLab)
                    .HasColumnType("numeric(18, 0)")
                    .HasColumnName("repOldLab");

                entity.Property(e => e.UpsizeTs)
                    .IsRowVersion()
                    .IsConcurrencyToken()
                    .HasColumnName("upsize_ts");
            });

            modelBuilder.Entity<TblLabresultdetail>(entity =>
            {
                entity.HasKey(e => new { e.SiteCode, e.LabrdId })
                    .HasName("tbl_LABRESULTDETAIL_PK")
                    .IsClustered(false);

                entity.ToTable("tbl_LABRESULTDETAIL", "pats");

                entity.Property(e => e.SiteCode)
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.LabrdId).HasColumnName("LABrdID");

                entity.Property(e => e.Labdetail)
                    .HasMaxLength(50)
                    .HasColumnName("LABDetail");

                entity.Property(e => e.LabrdFullNote).HasColumnName("LABrdFullNote");

                entity.Property(e => e.LabrdKey).HasColumnName("LABrdKey");

                entity.Property(e => e.LabrdNote).HasColumnName("LABrdNote");

                entity.Property(e => e.LabrdRecId).HasColumnName("LABrdRecID");

                entity.Property(e => e.LabrdResult)
                    .HasMaxLength(50)
                    .HasColumnName("LABrdResult");

                entity.Property(e => e.LabrdRx).HasColumnName("LABrdRx");

                entity.Property(e => e.LastModAt).HasColumnType("datetime");
            });
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}

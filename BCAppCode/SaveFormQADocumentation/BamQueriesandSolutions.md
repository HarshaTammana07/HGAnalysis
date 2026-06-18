--1) EXEC sp_helptext 'pats.vw_BAM';

CREATE view [pats].[vw_BAM] as  
-- added in case for Q5 to be blank if Q4 = 0  TC 20220519   
with cte_BAM1_5 as (  
select fid, sitecode, fCltID, date, clinicianTEXT, AdminLIST, IntervalLIST,  
q1Answer  as Q1, q2Answer  as Q2,q3Answer as Q3, q4Answer  as Q4,   
Case when q4Answer = '0' and q5Answer ='' then '0' else q5Answer end as Q5,   
q6Answer  as Q6,   
Case when q7aLIST like '%1%-%3%' then '1'  
    when q7aLIST like '%4%-%8%'then '2'   
    when q7aLIST like '%9%-%15%' then '3'   
    when q7aLIST like '%16%-%30%'then '4'  
    when q6Answer ='0' and q7aLIST ='' then '0' else q7aLIST end  as Q7a,  
Case when q7bLIST like '%1%-%3%' then '1'   
    when q7bLIST like '%4%-%8%'then '2'   
    when q7bLIST like '%9%-%15%' then '3'   
    when q7bLIST like '%16%-%30%'then '4'   
    when q6Answer ='0' and q7bLIST ='' then '0' else q7bLIST end as Q7b,  
Case when q7cLIST like '%1%-%3%' then '1'   
    when q7cLIST like '%4%-%8%'then '2'   
    when q7cLIST like '%9%-%15%' then '3'   
    when q7cLIST like '%16%-%30%'then '4'   
    when q6Answer ='0' and q7cLIST ='' then '0' else q7cLIST end  as Q7c,  
Case when q7dLIST like '%1%-%3%' then '1'   
    when q7dLIST like '%4%-%8%'then '2'   
    when q7dLIST like '%9%-%15%' then '3'   
    when q7dLIST like '%16%-%30%'then '4'   
    when q6Answer ='0' and q7dLIST ='' then '0' else q7dLIST end  as Q7d,  
Case when q7eLIST like '%1%-%3%' then '1 '  
    when q7eLIST like '%4%-%8%'then '2'   
    when q7eLIST like '%9%-%15%' then '3'   
    when q7eLIST like '%16%-%30%'then '4'   
    when q6Answer ='0' and q7eLIST ='' then '0' else q7eLIST end  as Q7e,  
Case when q7fLIST like '%1%-%3%' then '1'   
    when q7fLIST like '%4%-%8%'then '2'   
    when q7fLIST like '%9%-%15%' then '3'   
    when q7fLIST like '%16%-%30%'then '4'   
    when q6Answer ='0' and q7fLIST ='' then '0' else q7fLIST end  as Q7f,  
Case when q7gLIST like '%1%-%3%' then '1 '  
    when q7gLIST like '%4%-%8%'then '2'   
    when q7gLIST like '%9%-%15%' then '3'   
    when q7gLIST like '%16%-%30%'then '4'   
    when q6Answer ='0' and q7gLIST ='' then '0' else q7gLIST end as Q7g,  
q8Answer  as Q8, q9Answer  as Q9, q10Answer  as Q10,   
q11Answer  as Q11,q12Answer  as Q12, q13ANSWER  as Q13,   
q14ANSWER2  as Q14, q15ANSWER2  as Q15, q16ANSWER  as Q16,   
q17ANSWER  as Q17  
from [pats].[tbl_BriefAddictionMonitor]   
where RowState = 1 AND fCltID>0), ---153793  
cte_BAM2_5 as (  
select fid, sitecode, fCltID, date, clinicianTEXT, AdminLIST, IntervalLIST,  
  CAST(Q1 AS INT) Q1, CAST(Q2 AS INT) Q2, CAST(Q3 AS INT) Q3, CAST(Q4 AS INT) Q4, CAST(Q5 AS INT) Q5,   
  CAST(Q6 AS INT) Q6, CAST(Q7A AS INT) Q7a, CAST(Q7b AS INT) Q7b, CAST(Q7c AS INT) Q7c, CAST(Q7d AS INT) Q7d,   
  CAST(Q7e AS INT) Q7e, CAST(Q7f AS INT) Q7f, CAST(Q7g AS INT) Q7g, CAST(Q8 AS INT) Q8, CAST(Q9 AS INT) Q9,   
  CAST(Q10 AS INT) Q10, CAST(Q11 AS INT) Q11, CAST(Q12 AS INT) Q12, CAST(Q13 AS INT) Q13, CAST(Q14 AS INT) Q14,  
  CAST(Q15 AS INT) Q15,  CAST(Q16 AS INT) Q16,  CAST(Q17 AS INT) Q17,   
  UseCalc  = CAST(Q4 AS INT)+CAST(Q5 AS INT)+CAST(Q6 AS INT),   
  RiskCalc = CAST(Q1 AS INT)+ CAST(Q2 AS INT) +CAST(Q3 AS INT)+cAST(Q8 AS INT) +CAST(Q11 AS INT)+CAST(Q15 AS INT),  
  ProtectiveCalc = CAST(Q9 AS INT)+CAST(Q10 AS INT)+CAST(Q12 AS INT)+CAST(Q13 AS INT)+CAST(Q14 AS INT)+CAST(Q16 AS INT)  
from cte_BAM1_5  
  where    
    (Q1 in ('0','1','2','3','4') and Q2 in ('0','1','2','3','4') and Q3 in ('0','1','2','3','4') and Q4 in ('0','1','2','3','4')  
   and Q5 in ('0','1','2','3','4') and Q6 in ('0','1','2','3','4') and Q8 in ('0','1','2','3','4') and Q9 in ('0','1','2','3','4')  
   and Q10 in ('0','1','2','3','4') and Q11 in ('0','1','2','3','4') and Q12 in ('0','1','2','3','4') and Q13 in ('0','1','2','3','4')  
   and Q14 in ('0','4') and Q15 in ('0','1','2','3','4') and Q16 in ('0','1','2','3','4') and Q17 in ('0','1','2','3','4')  
   and Q7A in ('0','1','2','3','4') and Q7B in ('0','1','2','3','4') and Q7C in ('0','1','2','3','4') and Q7D in ('0','1','2','3','4') and   
   Q7E in ('0','1','2','3','4') and Q7F in ('0','1','2','3','4') and Q7G in ('0','1','2','3','4'))  
   ),  

cte_BAM1_6 as (  
   /*select *   
     from pats.tbl_dbo_FormQuestionAnswers   
    where FormName like '%Brief%' and Rowstate = 1  
--and SiteCode = b.SiteCode and Clientid = b.ClientId*/  
select qa.SiteCode, qa.FormName, qa.FormId, qa.ClientId, qa.CreatedOn, CreatedBy, UpdatedOn, UpdatedBy, qa.PreAdmissionId, qa.QuestionId, qa.QuestionOrderId, QuestionText, OptionId, AnswerValue  
      , qa.IsDeleted, qa.RowState, qa.LastModAt  
  from pats.tbl_dbo_FormQuestionAnswers qa   
  left join (select SiteCode, FormName, FormId, ClientId, PreAdmissionId, CreatedOn, QuestionId, Max(isnull(QuestionOrderId, 1)) QuestionOrderId  
    from pats.tbl_dbo_FormQuestionAnswers where FormName like '%Brief%' and Rowstate = 1   
    group by SiteCode, FormName, FormId, ClientId, PreAdmissionId, QuestionId, CreatedOn) qam   
   on (qa.SiteCode = qam.SiteCode and qa.FormName = qam.FormName and qa.FormId = qam.FormId and qa.ClientId = qam.ClientId and qa.PreAdmissionId = qam.PreAdmissionId  
       and qa.QuestionId = qam.QuestionId and qa.CreatedOn = qam.CreatedOn)  
where qa.FormName like '%Brief%' and Rowstate = 1 and qa.QuestionOrderId = qam.QuestionOrderId  
),  
cte_BAM2_6 as (  
Select f.Sitecode, f.formname, f.Formid, f.Clientid, f.CreatedOn, f.Createdby,   
f.UpdatedOn, f.UpdatedBy, f.PreAdmissionId, f.IsDeleted,f.RowState, f.LastmodAt, Min(f.AnswerValue) as AdminMethod  
from cte_BAM1_6 f   
where  f.QuestionId = '1720'  
group by f.Sitecode, f.formname, f.Formid, f.Clientid, f.CreatedOn, f.Createdby,   
f.UpdatedOn, f.UpdatedBy, f.PreAdmissionId, f.IsDeleted,f.RowState, f.LastmodAt),  
cte_BAM3_6 as (  
select b.* , bd.AnswerValue as EvalDate, bt.AnswerValue as StartTime, bte.AnswerValue as EndTime,   
b1.AnswerValue as Q1, b2.AnswerValue as Q2,b3.AnswerValue as Q3,b4.AnswerValue as Q4,  
b5.AnswerValue as Q5, b6.AnswerValue as Q6, b7a.AnswerValue as Q7a,b7b.AnswerValue as Q7b,b7c.AnswerValue as Q7c,  
b7d.AnswerValue as Q7d,b7e.AnswerValue as Q7e,b7f.AnswerValue as Q7f,b7g.AnswerValue as Q7g,b8.AnswerValue as Q8,b9.AnswerValue as Q9,  
b10.AnswerValue as Q10, b11.AnswerValue as Q11,b12.AnswerValue as Q12,b13.AnswerValue as Q13,b14.AnswerValue as Q14, b15.AnswerValue as Q15,  
b16.AnswerValue as Q16, b17.AnswerValue as Q17  
from cte_BAM2_6 b  
left join cte_BAM1_6 bt on b.SiteCode = bt.SiteCode and b.ClientId = bt.ClientId and b.PreAdmissionId = bt.PreAdmissionId and b.CreatedOn = bt.CreatedOn  
and bt.QuestionId = 1721    
left join cte_BAM1_6 bd on b.SiteCode = bd.SiteCode and b.ClientId = bd.ClientId and b.PreAdmissionId = bd.PreAdmissionId and b.CreatedOn = bd.CreatedOn  
and bd.QuestionId = 1718  
left join cte_BAM1_6 b1 on b.SiteCode = b1.SiteCode and b.ClientId = b1.ClientId and b.PreAdmissionId = b1.PreAdmissionId and b.CreatedOn = b1.CreatedOn  
and b1.QuestionId = 1722  
left join cte_BAM1_6 b2 on b.SiteCode = b2.SiteCode and b.ClientId = b2.ClientId and b.PreAdmissionId = b2.PreAdmissionId and b.CreatedOn = b2.CreatedOn  
and b2.QuestionId = 1723  
left join cte_BAM1_6 b3 on b.SiteCode = b3.SiteCode and b.ClientId = b3.ClientId and b.PreAdmissionId = b3.PreAdmissionId and b.CreatedOn = b3.CreatedOn  
and b3.QuestionId = 1724  
left join cte_BAM1_6 b4 on b.SiteCode = b4.SiteCode and b.ClientId = b4.ClientId and b.PreAdmissionId = b4.PreAdmissionId and b.CreatedOn = b4.CreatedOn  
and b4.QuestionId = 1725  
left join cte_BAM1_6 b5 on b.SiteCode = b5.SiteCode and b.ClientId = b5.ClientId and b.PreAdmissionId = b5.PreAdmissionId and b.CreatedOn = b5.CreatedOn  
and b5.QuestionId = 1726  
left join cte_BAM1_6 b6 on b.SiteCode = b6.SiteCode and b.ClientId = b6.ClientId and b.PreAdmissionId = b6.PreAdmissionId and b.CreatedOn = b6.CreatedOn  
and b6.QuestionId = 1727  
left join cte_BAM1_6 b7a on b.SiteCode = b7a.SiteCode and b.ClientId = b7a.ClientId and b.PreAdmissionId = b7a.PreAdmissionId and b.CreatedOn = b7a.CreatedOn  
and b7a.QuestionId = 1740  
left join cte_BAM1_6 b7b on b.SiteCode = b7b.SiteCode and b.ClientId = b7b.ClientId and b.PreAdmissionId = b7b.PreAdmissionId and b.CreatedOn = b7b.CreatedOn  
and b7b.QuestionId = 1741  
left join cte_BAM1_6 b7c on b.SiteCode = b7c.SiteCode and b.ClientId = b7c.ClientId and b.PreAdmissionId = b7c.PreAdmissionId and b.CreatedOn = b7c.CreatedOn  
and b7c.QuestionId = 1742  
left join cte_BAM1_6 b7d on b.SiteCode = b7d.SiteCode and b.ClientId = b7d.ClientId and b.PreAdmissionId = b7d.PreAdmissionId and b.CreatedOn = b7d.CreatedOn  
  and b7d.QuestionId = 1743  
left join cte_BAM1_6 b7e on b.SiteCode = b7e.SiteCode and b.ClientId = b7e.ClientId and b.PreAdmissionId = b7e.PreAdmissionId and b.CreatedOn = b7e.CreatedOn  
and b7e.QuestionId = 1744  
left join cte_BAM1_6 b7f on b.SiteCode = b7f.SiteCode and b.ClientId = b7f.ClientId and b.PreAdmissionId = b7f.PreAdmissionId and b.CreatedOn = b7f.CreatedOn  
and b7f.QuestionId = 1745  
left join cte_BAM1_6 b7g on b.SiteCode = b7g.SiteCode and b.ClientId = b7g.ClientId and b.PreAdmissionId = b7g.PreAdmissionId and b.CreatedOn = b7g.CreatedOn  
  and b7g.QuestionId = 1746  
left join cte_BAM1_6 b8 on b.SiteCode = b8.SiteCode and b.ClientId = b8.ClientId and b.PreAdmissionId = b8.PreAdmissionId and b.CreatedOn = b8.CreatedOn  
and b8.QuestionId = 1729  
left join cte_BAM1_6 b9 on b.SiteCode = b9.SiteCode and b.ClientId = b9.ClientId and b.PreAdmissionId = b9.PreAdmissionId and b.CreatedOn = b9.CreatedOn  
and b9.QuestionId = 1730  
left join cte_BAM1_6 b10 on b.SiteCode = b10.SiteCode and b.ClientId = b10.ClientId and b.PreAdmissionId = b10.PreAdmissionId and b.CreatedOn = b10.CreatedOn  
and b10.QuestionId = 1731  
left join cte_BAM1_6 b11 on b.SiteCode = b11.SiteCode and b.ClientId = b11.ClientId and b.PreAdmissionId = b11.PreAdmissionId and b.CreatedOn = b11.CreatedOn  
and b11.QuestionId = 1732  
left join cte_BAM1_6 b12 on b.SiteCode = b12.SiteCode and b.ClientId = b12.ClientId and b.PreAdmissionId = b12.PreAdmissionId and b.CreatedOn = b12.CreatedOn  
and b12.QuestionId = 1733  
left join cte_BAM1_6 b13 on b.SiteCode = b13.SiteCode and b.ClientId = b13.ClientId and b.PreAdmissionId = b13.PreAdmissionId and b.CreatedOn = b13.CreatedOn  
and b13.QuestionId = 1734  
left join cte_BAM1_6 b14 on b.SiteCode = b14.SiteCode and b.ClientId = b14.ClientId and b.PreAdmissionId = b14.PreAdmissionId and b.CreatedOn = b14.CreatedOn  
and b14.QuestionId = 1736  
left join cte_BAM1_6 b15 on b.SiteCode = b15.SiteCode and b.ClientId = b15.ClientId and b.PreAdmissionId = b15.PreAdmissionId and b.CreatedOn = b15.CreatedOn  
and b15.QuestionId = 1735  
left join cte_BAM1_6 b16 on b.SiteCode = b16.SiteCode and b.ClientId = b16.ClientId and b.PreAdmissionId = b16.PreAdmissionId and b.CreatedOn = b16.CreatedOn  
and b16.QuestionId = 1737  
left join cte_BAM1_6 b17 on b.SiteCode = b17.SiteCode and b.ClientId = b17.ClientId and b.PreAdmissionId = b17.PreAdmissionId and b.CreatedOn = b17.CreatedOn  
and b17.QuestionId = 1738  
left join cte_BAM1_6 bte on b.SiteCode = bte.SiteCode and b.ClientId = bte.ClientId and b.PreAdmissionId = bte.PreAdmissionId and b.CreatedOn = bte.CreatedOn  
and bte.QuestionId = 1739),  
cte_BAM4_6 as (  
select  'BAMv6.2' as form, sitecode, ClientId, CreatedOn, IsNull(UpdatedBy,CreatedBy) as Clinician, AdminMethod, PreAdmissionId,  EvalDate, StartTime, EndTime,  
Cast(Case when left(Q1,4) = 'Exce' then 0 when left(Q1,9) = 'Very Good' then 1 when left(Q1,4) = 'Good' then 2 when left(Q1,4) = 'Fair' then 3 when left(Q1,4) = 'Poor' then 4 else Q1 end as varchar(20)) as Q1, Q1 as Q1orig,  
Cast(Case when left(Q2,1) = '0' then 0 when left(Q2,4) = '1-3 ' then 1 when left(Q2,1) = '4' then 2 when left(Q2,1) = '9' then 3 when left(Q2,2) = '16' then 4 else Q2 end as varchar(20))  as Q2,  Q2 as Q2orig,  
Cast(Case when left(Q3,1) = '0' then 0 when left(Q3,4) = '1-3 ' then 1 when left(Q3,1) = '4' then 2 when left(Q3,1) = '9' then 3 when left(Q3,2) = '16' then 4 else Q3 end  as varchar(20)) as Q3,  Q3 as Q3orig ,  
Cast(Case when left(Q4,1) = '0' then 0 when left(Q4,4) = '1-3 ' then 1 when left(Q4,1) = '4' then 2 when left(Q4,1) = '9' then 3 when left(Q4,2) = '16' then 4 else Q4 end  as varchar(20)) as Q4,  Q4 as Q4orig,   
Cast(Case when Q4  = '0 (Skip to #6) (0)' and Q5 is null then '0' when left(Q5,1) = '0' then 0 when left(Q5,4) = '1-3 ' then 1 when left(Q5,1) = '4' then 2 when left(Q5,1) = '9' then 3 when left(Q5,2) = '16' then 4 else Q5 end as varchar(20)) as Q5,  Q5 
as Q5orig,   
Cast(Case when left(Q6,1) = '0' then 0 when left(Q6,4) = '1-3 ' then 1 when left(Q6,1) = '4' then 2 when left(Q6,1) = '9' then 3 when left(Q6,2) = '16' then 4 else Q6 end as varchar(20))  as Q6,  Q6 as Q6orig,   
Cast(Case when Q6 = '0 (Skip to #8) (0)' and  Q7a is null then '0' when Q7a like '%1%-%3%' then '1 ' when Q7a like '%4%-%8%'then '2' when Q7a like '%9%-%15%' then '3' when Q7a like '%16%-%30%'then '4' else Q7a end as varchar(20))  as Q7a, Q7a as Q7aorig,
Cast(Case when Q6 = '0 (Skip to #8) (0)' and  Q7b is null then '0' when Q7b like '%1%-%3%' then '1 ' when Q7b like '%4%-%8%'then '2' when Q7b like '%9%-%15%' then '3' when Q7b like '%16%-%30%'then '4' else Q7b end as varchar(20)) as Q7b, Q7b as Q7borig, 
Cast(Case when Q6 = '0 (Skip to #8) (0)' and  Q7c is null then '0' when Q7c like '%1%-%3%' then '1 ' when Q7c like '%4%-%8%'then '2' when Q7c like '%9%-%15%' then '3' when Q7c like '%16%-%30%'then '4' else Q7c end as varchar(20)) as Q7c, Q7c as Q7corig, 
Cast(Case when Q6 = '0 (Skip to #8) (0)' and  Q7d is null then '0' when Q7d like '%1%-%3%' then '1 ' when Q7d like '%4%-%8%'then '2' when Q7d like '%9%-%15%' then '3' when Q7d like '%16%-%30%'then '4' else Q7d end as varchar(20)) as Q7d, Q7d as Q7dorig, 
Cast(Case when Q6 = '0 (Skip to #8) (0)' and  Q7e is null then '0' when Q7e like '%1%-%3%' then '1 ' when Q7e like '%4%-%8%'then '2' when Q7e like '%9%-%15%' then '3' when Q7e like '%16%-%30%'then '4' else Q7e end as varchar(20)) as Q7e, Q7e as Q7eorig, 
Cast(Case when Q6 = '0 (Skip to #8) (0)' and  Q7f is null then '0' when Q7f like '%1%-%3%' then '1 ' when Q7f like '%4%-%8%'then '2' when Q7f like '%9%-%15%' then '3' when Q7f like '%16%-%30%'then '4' else Q7f end as varchar(20)) as Q7f, Q7f as Q7forig, 
Cast(Case when Q6 = '0 (Skip to #8) (0)' and  Q7g is null then '0' when Q7g like '%1%-%3%' then '1 ' when Q7g like '%4%-%8%'then '2' when Q7g like '%9%-%15%' then '3' when Q7g like '%16%-%30%'then '4' else Q7g end as varchar(20)) as Q7g, Q7g as Q7gorig, 

Cast(Case when left(Q8,10) = 'Not at all' then 0 when left(Q8,8) = 'Slightly' then 1 when left(Q8,10) = 'Moderately' then 2 when left(Q8,8) = 'Consider' then 3 when left(Q8,9) = 'Extremely' then 4 else Q8 end as varchar(20))  as Q8,  Q8 as Q8orig,  
Cast(Case when left(Q9,10) = 'Not at all' then 0 when left(Q9,8) = 'Slightly' then 1 when left(Q9,10) = 'Moderately' then 2 when left(Q9,8) = 'Consider' then 3 when left(Q9,9) = 'Extremely' then 4 else Q9 end  as varchar(20)) as Q9,  Q9 as Q9orig,  
Cast(Case when left(Q10,1) = '0' then 0 when left(Q10,4) = '1-3 ' then 1 when left(Q10,1) = '4' then 2 when left(Q10,1) = '9' then 3 when left(Q10,2) = '16' then 4 else Q10 end  as varchar(20)) as Q10,  Q10   as Q10orig,  
Cast(Case when left(Q11,1) = '0' then 0 when left(Q11,4) = '1-3 ' then 1 when left(Q11,1) = '4' then 2 when left(Q11,1) = '9' then 3 when left(Q11,2) = '16' then 4 else Q11 end  as varchar(20)) as Q11,  Q11   as Q11orig,  
Cast(Case when left(Q12,10) = 'Not at all' then 0 when left(Q12,8) = 'Slightly' then 1 when left(Q12,10) = 'Moderately' then 2 when left(Q12,8) = 'Consider' then 3 when left(Q12,9) = 'Extremely' then 4 else Q12 end as varchar(20))  as Q12,  Q12 as Q12ori
g,  
Cast(Case when left(Q13,1) = '0' then 0 when left(Q13,4) = '1-3 ' then 1 when left(Q13,1) = '4' then 2 when left(Q13,1) = '9' then 3 when left(Q13,2) = '16' then 4 else Q13 end  as varchar(20)) as Q13,  Q13  as Q13orig,  
Cast(case when left(Q14,3) = 'Yes' then 4 when left(Q14,2) = 'No' then 0 else Q14 end as varchar(20)) as Q14, Q14 as Q14orig,   
Cast(Case when left(Q15,10) = 'Not at all' then 0 when left(Q15,8) = 'Slightly' then 1 when left(Q15,10) = 'Moderately' then 2 when left(Q15,8) = 'Consider' then 3 when left(Q15,9) = 'Extremely' then 4 else Q15 end  as varchar(20))  as Q15,  Q15 as Q15or
ig,   
Cast(Case when left(Q16,1) = '0' then 0 when left(Q16,4) = '1-3 ' then 1 when left(Q16,1) = '4' then 2 when left(Q16,1) = '9' then 3 when left(Q16,2) = '16' then 4 else Q16 end as varchar(20))  as Q16,  Q16 as Q16orig,  
Cast(Case when left(Q17,10) = 'Not at all' then 4 when left(Q17,8) = 'Slightly' then 3 when left(Q17,10) = 'Moderately' then 2 when left(Q17,8) = 'Consider' then 1 when left(Q17,9) = 'Extremely' then 0 else Q17 end  as varchar(20)) as Q17,  Q17 as Q17ori
g  
   from cte_BAM3_6),  
--New BAM table starting in Nov 2025  
cte_BAM2025 as (  
select id, sitecode, ClientId as fcltID, BAMdate as Date, CreatedBy as Clinician, case when ClinicianInterview=1 or Phone=1 then 'Interview' else 'Self Completed' end  as AdminMethod, null as IntervalLIST,  
InstructionsQ1  as Q1, InstructionsQ2 as Q2,InstructionsQ3 as Q3,   
instructionsQ4 as Q4,   
instructionsQ5 as Q5,   
instructionsQ6  as Q6,   
instructionsQ7a as Q7a,  
instructionsQ7b as Q7b,  
instructionsQ7c as Q7c,  
instructionsQ7d as Q7d,  
instructionsQ7e as Q7e,  
instructionsQ7f as Q7f,  
instructionsQ7g as Q7g,  
InstructionsQ8  as Q8, InstructionsQ9  as Q9, InstructionsQ10  as Q10,   
InstructionsQ11  as Q11,InstructionsQ12  as Q12, InstructionsQ13 as Q13,   
InstructionsQ14  as Q14, InstructionsQ15  as Q15, InstructionsQ16  as Q16,   
InstructionsQ17  as Q17  
from [pats].[tbl_BAMForm]   
where IsDeleted= 0 AND ClientId>0 and bamdate is not null), ---153793  
cte_BAM2025_2 as (  
select id, sitecode, fCltID, date, clinician, Adminmethod, IntervalLIST,  
  CAST(Q1 AS INT) Q1, CAST(Q2 AS INT) Q2, CAST(Q3 AS INT) Q3, CAST(Q4 AS INT) Q4, CAST(Q5 AS INT) Q5,   
  CAST(Q6 AS INT) Q6, CAST(Q7A AS INT) Q7a, CAST(Q7b AS INT) Q7b, CAST(Q7c AS INT) Q7c, CAST(Q7d AS INT) Q7d,   
  CAST(Q7e AS INT) Q7e, CAST(Q7f AS INT) Q7f, CAST(Q7g AS INT) Q7g, CAST(Q8 AS INT) Q8, CAST(Q9 AS INT) Q9,   
  CAST(Q10 AS INT) Q10, CAST(Q11 AS INT) Q11, CAST(Q12 AS INT) Q12, CAST(Q13 AS INT) Q13, CAST(Q14 AS INT) Q14,  
  CAST(Q15 AS INT) Q15,  CAST(Q16 AS INT) Q16,  CAST(Q17 AS INT) Q17,   
  UseCalc  = CAST(Q4 AS INT)+CAST(Q5 AS INT)+CAST(Q6 AS INT),   
  RiskCalc = CAST(Q1 AS INT)+ CAST(Q2 AS INT) +CAST(Q3 AS INT)+cAST(Q8 AS INT) +CAST(Q11 AS INT)+CAST(Q15 AS INT),  
  ProtectiveCalc = CAST(Q9 AS INT)+CAST(Q10 AS INT)+CAST(Q12 AS INT)+CAST(Q13 AS INT)+CAST(Q14 AS INT)+CAST(Q16 AS INT)  
from cte_BAM2025  
  where    
    (Q1 in ('0','1','2','3','4') and Q2 in ('0','1','2','3','4') and Q3 in ('0','1','2','3','4') and Q4 in ('0','1','2','3','4')  
   and Q5 in ('0','1','2','3','4') and Q6 in ('0','1','2','3','4') and Q8 in ('0','1','2','3','4') and Q9 in ('0','1','2','3','4')  
   and Q10 in ('0','1','2','3','4') and Q11 in ('0','1','2','3','4') and Q12 in ('0','1','2','3','4') and Q13 in ('0','1','2','3','4')  
   and Q14 in ('0','4') and Q15 in ('0','1','2','3','4') and Q16 in ('0','1','2','3','4') and Q17 in ('0','1','2','3','4')  
   and Q7A in ('0','1','2','3','4') and Q7B in ('0','1','2','3','4') and Q7C in ('0','1','2','3','4') and Q7D in ('0','1','2','3','4') and   
   Q7E in ('0','1','2','3','4') and Q7F in ('0','1','2','3','4') and Q7G in ('0','1','2','3','4'))  
   ),  
  Cte_BAM5_6 AS (  
   select *,     
  UseCalc  = CAST(Q4 AS INT)+CAST(Q5 AS INT)+CAST(Q6 AS INT),   
  RiskCalc = CAST(Q1 AS INT)+ CAST(Q2 AS INT) +CAST(Q3 AS INT)+cAST(Q8 AS INT) +CAST(Q11 AS INT)+CAST(Q15 AS INT),  
  ProtectiveCalc = CAST(Q9 AS INT)+CAST(Q10 AS INT)+CAST(Q12 AS INT)+CAST(Q13 AS INT)+CAST(Q14 AS INT)+CAST(Q16 AS INT)   
  from cte_BAM4_6  
   where   (Q1 in ('0','1','2','3','4') and Q2 in ('0','1','2','3','4') and Q3 in ('0','1','2','3','4') and Q4 in ('0','1','2','3','4')  
   and Q5 in ('0','1','2','3','4') and Q6 in ('0','1','2','3','4') and Q8 in ('0','1','2','3','4') and Q9 in ('0','1','2','3','4')  
   and Q10 in ('0','1','2','3','4') and Q11 in ('0','1','2','3','4') and Q12 in ('0','1','2','3','4') and Q13 in ('0','1','2','3','4')  
   and Q14 in ('0','4') and Q15 in ('0','1','2','3','4') and Q16 in ('0','1','2','3','4') and Q17 in ('0','1','2','3','4')  
   and Q7A in ('0','1','2','3','4') and Q7B in ('0','1','2','3','4') and Q7C in ('0','1','2','3','4') and Q7D in ('0','1','2','3','4') and   
   Q7E in ('0','1','2','3','4') and Q7F in ('0','1','2','3','4') and Q7G in ('0','1','2','3','4')))  

Select Sitecode, Clientid as fcltid, Createdon as [Date], Clinician as ClinicianTEXT, AdminMethod as AdminList, PreAdmissionId, cast(Null as int) as fid,   
CAST(Q1 AS INT) Q1, CAST(Q2 AS INT) Q2, CAST(Q3 AS INT) Q3, CAST(Q4 AS INT) Q4, CAST(Q5 AS INT) Q5,   
  CAST(Q6 AS INT) Q6, CAST(Q7A AS INT) Q7a, CAST(Q7b AS INT) Q7b, CAST(Q7c AS INT) Q7c, CAST(Q7d AS INT) Q7d,   
  CAST(Q7e AS INT) Q7e, CAST(Q7f AS INT) Q7f, CAST(Q7g AS INT) Q7g, CAST(Q8 AS INT) Q8, CAST(Q9 AS INT) Q9,   
  CAST(Q10 AS INT) Q10, CAST(Q11 AS INT) Q11, CAST(Q12 AS INT) Q12, CAST(Q13 AS INT) Q13, CAST(Q14 AS INT) Q14,  
  CAST(Q15 AS INT) Q15,  CAST(Q16 AS INT) Q16,  CAST(Q17 AS INT) Q17,  
  UseCalc, RiskCalc, ProtectiveCalc  
  FROM Cte_BAM5_6  
UNION  

Select Sitecode, fcltid, [Date], ClinicianTEXT , AdminList , Null as PreAdmissionId, fid,   
CAST(Q1 AS INT) as Q1, CAST(Q2 AS INT) as Q2, CAST(Q3 AS INT) as Q3, CAST(Q4 AS INT)  as Q4, CAST(Q5 AS INT) as  Q5,   
  CAST(Q6 AS INT) as Q6, CAST(Q7A AS INT) as Q7a, CAST(Q7b AS INT)as Q7b, CAST(Q7c AS INT) as Q7c, CAST(Q7d AS INT) as  Q7d,   
  CAST(Q7e AS INT) as Q7e, CAST(Q7f AS INT)as Q7f, CAST(Q7g AS INT) as Q7g, CAST(Q8 AS INT) as Q8, CAST(Q9 AS INT) as  Q9,   
  CAST(Q10 AS INT) as  Q10, CAST(Q11 AS INT) as  Q11, CAST(Q12 AS INT) as  Q12, CAST(Q13 AS INT) as  Q13, CAST(Q14 AS INT) as  Q14,  
  CAST(Q15 AS INT) as  Q15,  CAST(Q16 AS INT) as  Q16,  CAST(Q17 AS INT) as  Q17,  
  UseCalc, RiskCalc, ProtectiveCalc  
from  cte_BAM2_5  
UNION  

Select Sitecode, fcltid as Clientid, [Date], clinician, AdminMethod, Null as PreAdmissionId, id as fid,   
CAST(Q1 AS INT) as Q1, CAST(Q2 AS INT) as Q2, CAST(Q3 AS INT) as Q3, CAST(Q4 AS INT)  as Q4, CAST(Q5 AS INT) as  Q5,   
  CAST(Q6 AS INT) as Q6, CAST(Q7A AS INT) as Q7a, CAST(Q7b AS INT)as Q7b, CAST(Q7c AS INT) as Q7c, CAST(Q7d AS INT) as  Q7d,   
  CAST(Q7e AS INT) as Q7e, CAST(Q7f AS INT)as Q7f, CAST(Q7g AS INT) as Q7g, CAST(Q8 AS INT) as Q8, CAST(Q9 AS INT) as  Q9,   
  CAST(Q10 AS INT) as  Q10, CAST(Q11 AS INT) as  Q11, CAST(Q12 AS INT) as  Q12, CAST(Q13 AS INT) as  Q13, CAST(Q14 AS INT) as  Q14,  
  CAST(Q15 AS INT) as  Q15,  CAST(Q16 AS INT) as  Q16,  CAST(Q17 AS INT) as  Q17,  
  UseCalc, RiskCalc, ProtectiveCalc  
from  cte_BAM2025_2  

 
 --2. Full stored procedure definition, if accessible
EXEC sp_helptext 'pats.BAMMerge';
CREATE procedure [pats].[BAMMerge]   
(  
@sitecode varchar(25)  
) as   
DECLARE @SummaryOfChanges TABLE(Change VARCHAR(20));   
delete from pats.tbl_vw_BAM where SiteCode = @sitecode;  
Merge into pats.tbl_vw_BAM as t  
Using (SELECT [Sitecode], [fcltid], [Date], Idx = Row_Number() over(Partition by SiteCode, fcltid, [Date] order by SiteCode, fcltid, [Date], ClinicianTEXT, AdminList, PreAdmissionId, fid)  
      , [ClinicianTEXT], [AdminList], [PreAdmissionId], [fid]  
      , [Q1], [Q2], [Q3], [Q4], [Q5], [Q6], [Q7a], [Q7b], [Q7c], [Q7d], [Q7e], [Q7f], [Q7g], [Q8]  
      , [Q9], [Q10], [Q11], [Q12], [Q13], [Q14], [Q15], [Q16], [Q17], [UseCalc], [RiskCalc]  
      , [ProtectiveCalc]  
  FROM [pats].[vw_BAM]   
  where SiteCode = @sitecode   
  ) as s  
    on (t.SiteCode = s.SiteCode and t.cltid = s.fcltid and t.[Date] = s.[Date] and t.Idx = s.Idx)  
When Matched Then   
update set t.[ClinicianTEXT] = s.ClinicianTEXT  
      , t.[AdminList] = s.[AdminList]  
      , t.[PreAdmissionId] = s.[PreAdmissionId], t.fid = s.fid  
      , t.[Q1] = s.[Q1]  
      , t.[Q2] = s.[Q2]  
      , t.[Q3] = s.[Q3]  
      , t.[Q4] = s.[Q4]  
      , t.[Q5] = s.[Q5]  
      , t.[Q6] = s.[Q6]  
      , t.[Q7a] = s.[Q7a]  
      , t.[Q7b] = s.[Q7b]  
      , t.[Q7c] = s.[Q7c]  
      , t.[Q7d] = s.[Q7d]  
      , t.[Q7e] = s.[Q7e]  
      , t.[Q7f] = s.[Q7f]  
      , t.[Q7g] = s.[Q7g]  
      , t.[Q8] = s.[Q8]  
      , t.[Q9] = s.[Q9]  
      , t.[Q10] = s.[Q10]  
      , t.[Q11] = s.[Q11]  
      , t.[Q12] = s.[Q12]  
      , t.[Q13] = s.[Q13]  
      , t.[Q14] = s.[Q14]  
      , t.[Q15] = s.[Q15]  
      , t.[Q16] = s.[Q16]  
      , t.[Q17] = s.[Q17]  
      , t.[UseCalc] = s.[UseCalc]  
      , t.[RiskCalc] = s.[RiskCalc]  
      , t.[ProtectiveCalc] = s.[ProtectiveCalc]  
when not Matched by TARGET and s.[fcltID] > 0 then  
    Insert ([Sitecode], [cltid], [Date], Idx, [ClinicianTEXT], [AdminList], [PreAdmissionId], [fid]  
          , [Q1], [Q2], [Q3], [Q4], [Q5], [Q6], [Q7a], [Q7b], [Q7c], [Q7d], [Q7e], [Q7f], [Q7g]  
          , [Q8], [Q9], [Q10], [Q11], [Q12], [Q13], [Q14], [Q15], [Q16], [Q17], [UseCalc]  
          , [RiskCalc], [ProtectiveCalc])  
   values (s.[Sitecode], s.[fcltid], s.[Date], s.Idx, s.[ClinicianTEXT], s.[AdminList], s.[PreAdmissionId], s.[fid]  
            , s.[Q1], s.[Q2], s.[Q3], s.[Q4], s.[Q5], s.[Q6], s.[Q7a], s.[Q7b], s.[Q7c], s.[Q7d], s.[Q7e], s.[Q7f]  
   , s.[Q7g], s.[Q8], s.[Q9], s.[Q10], s.[Q11], s.[Q12], s.[Q13], s.[Q14], s.[Q15], s.[Q16], s.[Q17]  
            , s.[UseCalc], s.[RiskCalc], s.[ProtectiveCalc])  

--When not Matched by SOURCE and t.SiteCode = s.SiteCode then   
--  update set t.RowState = 0  
output $action into @SummaryOfChanges;  
--if(@sitecode = 'V9')  
  --begin  
--delete FROM [pats].[tbl_vw_BAM] where siteCode = 'V9' and cltid = 43147 and [date] = '2023-1-2';  
  --end  
select *  
     /*  RowsUpd = count(case when Change = 'UPDATE' then 1 else 0 end)  
     , RowsIns = count(case when Change = 'INSERT' then 1 else 0 end) */  
  from @SummaryOfChanges;  
 
 --3.) Target table schema
SELECT
    c.column_id,
    c.name AS column_name,
    t.name AS data_type,
    c.max_length,
    c.precision,
    c.scale,
    c.is_nullable
FROM sys.columns c
JOIN sys.types t
    ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('pats.tbl_vw_BAM')
ORDER BY c.column_id;

column_id	column_name	data_type	max_length	precision	scale	is_nullable
1	Sitecode	varchar	25	0	0	0
2	cltid	int	4	10	0	0
3	Date	date	3	10	0	0
4	Idx	int	4	10	0	0
5	ClinicianTEXT	varchar	300	0	0	1
6	AdminList	varchar	300	0	0	1
7	PreAdmissionId	int	4	10	0	1
8	fid	int	4	10	0	1
9	Q1	int	4	10	0	1
10	Q2	int	4	10	0	1
11	Q3	int	4	10	0	1
12	Q4	int	4	10	0	1
13	Q5	int	4	10	0	1
14	Q6	int	4	10	0	1
15	Q7a	int	4	10	0	1
16	Q7b	int	4	10	0	1
17	Q7c	int	4	10	0	1
18	Q7d	int	4	10	0	1
19	Q7e	int	4	10	0	1
20	Q7f	int	4	10	0	1
21	Q7g	int	4	10	0	1
22	Q8	int	4	10	0	1
23	Q9	int	4	10	0	1
24	Q10	int	4	10	0	1
25	Q11	int	4	10	0	1
26	Q12	int	4	10	0	1
27	Q13	int	4	10	0	1
28	Q14	int	4	10	0	1
29	Q15	int	4	10	0	1
30	Q16	int	4	10	0	1
31	Q17	int	4	10	0	1
32	UseCalc	int	4	10	0	1
33	RiskCalc	int	4	10	0	1
34	ProtectiveCalc	int	4	10	0	1
 
 --4). Primary key / indexes on target table
SELECT
    i.name AS index_name,
    i.type_desc,
    i.is_primary_key,
    i.is_unique,
    ic.key_ordinal,
    c.name AS column_name
FROM sys.indexes i
JOIN sys.index_columns ic
    ON i.object_id = ic.object_id
   AND i.index_id = ic.index_id
JOIN sys.columns c
    ON ic.object_id = c.object_id
   AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('pats.tbl_vw_BAM')
ORDER BY i.name, ic.key_ordinal;

index_name	type_desc	is_primary_key	is_unique	key_ordinal	column_name
PK_tbl_vw_BAM	CLUSTERED	1	1	1	Sitecode
PK_tbl_vw_BAM	CLUSTERED	1	1	2	cltid
PK_tbl_vw_BAM	CLUSTERED	1	1	3	Date
PK_tbl_vw_BAM	CLUSTERED	1	1	4	Idx

 -- 5). BAM-related FormName values present in FormQuestionAnswers
SELECT
    FormName,
    COUNT(*) AS row_count,
    MIN(CreatedOn) AS min_created_on,
    MAX(CreatedOn) AS max_created_on
FROM pats.tbl_dbo_FormQuestionAnswers
WHERE FormName LIKE '%BAM%'
   OR FormName LIKE '%Brief Addiction%'
GROUP BY FormName
ORDER BY row_count DESC;

FormName	row_count	min_created_on	max_created_on
Brief Addiction Monitor (BAM) With Scoring & Clinical Guidelines	7222712	2021-08-02	2026-05-13

 
-- 6). Sample BAM rows from raw FormQuestionAnswers
SELECT TOP 100
    SiteCode,
    FormName,
    FormId,
    ClientId,
    PreAdmissionId,
    CreatedOn,
    UpdatedOn,
    QuestionId,
    QuestionOrderId,
    QuestionText,
    OptionId,
    AnswerValue,
    RowState,
    IsDeleted
FROM pats.tbl_dbo_FormQuestionAnswers
WHERE FormName LIKE '%BAM%'
   OR FormName LIKE '%Brief Addiction%'
ORDER BY SiteCode, FormId, QuestionOrderId, QuestionId;

 AHK	Brief Addiction Monitor (BAM) With Scoring & Clinical Guidelines	0080717F-C545-4143-A4A2-E9A2F5AE6D6B	237	13627	2025-12-11	NULL	1718	1	Date:</b>	2235	2025-12-10	1	NULL

-- 7). Sample output rows currently produced by BAMMerge
SELECT TOP 100 *
FROM pats.tbl_vw_BAM
ORDER BY SiteCode, cltid, [Date], Idx;

 Sitecode	cltid	Date	Idx	ClinicianTEXT	AdminList	PreAdmissionId	fid	Q1	Q2	Q3	Q4	Q5	Q6	Q7a	Q7b	Q7c	Q7d	Q7e	Q7f	Q7g	Q8	Q9	Q10	Q11	Q12	Q13	Q14	Q15	Q16	Q17	UseCalc	RiskCalc	ProtectiveCalc
AHK	2	2024-11-20	1	Jimmy Vaughan	Clinician Interview	2310	NULL	3	1	0	2	1	2	3	0	1	0	3	0	0	2	1	0	2	1	2	4	0	4	3	5	8	12
AHK	2	2025-09-08	1	Jimmy Vaughan	Clinician Interview	11654	NULL	2	0	0	1	0	2	2	0	2	0	3	0	0	3	1	0	2	1	2	0	0	4	3	3	7	8
AHK	2	2026-01-22	1	Jimmy Vaughan	Interview	NULL	5	2	2	0	0	0	3	3	0	3	0	3	0	0	3	1	0	2	1	3	0	0	4	1	3	9	9
AHK	3	2024-08-19	1	Jimmy Vaughan	Phone	1327	NULL	2	1	1	0	0	0	0	0	0	0	3	0	0	3	2	0	0	0	0	0	1	1	2	0	8	3
AHK	5	2024-12-18	1	Jimmy Vaughan	Clinician Interview	2636	NULL	2	1	0	0	0	0	0	0	0	0	0	0	0	0	1	0	0	2	3	4	0	4	2	0	3	14
AHK	5	2025-01-30	1	Jimmy Vaughan	Clinician Interview	3984	NULL	2	0	0	0	0	0	0	0	0	0	0	0	0	0	2	0	0	1	4	4	0	4	1	0	2	15
AHK	5	2025-09-08	1	Jimmy Vaughan	Clinician Interview	11656	NULL	2	0	0	0	0	0	0	0	0	0	0	0	0	0	3	0	0	1	4	4	0	4	1	0	2	16
AHK	5	2026-03-18	1	Jimmy Vaughan	Interview	NULL	33	2	0	0	0	0	0	0	0	0	0	0	0	0	0	4	0	0	2	4	4	0	4	4	0	2	18
AHK	6	2024-12-12	1	Traci Etheridge	Clinician Interview	2562	NULL	2	0	4	0	0	2	0	1	0	0	2	0	0	2	3	0	4	2	4	4	0	4	1	2	12	17
AHK	6	2025-02-11	1	Traci Etheridge	Clinician Interview	4158	NULL	2	0	0	1	0	4	0	0	0	0	1	0	0	1	3	1	1	2	4	4	0	4	1	5	4	18

-- 8). Row counts by site in BAM target
SELECT
    SiteCode,
    COUNT(*) AS row_count,
    MIN([Date]) AS min_date,
    MAX([Date]) AS max_date
FROM pats.tbl_vw_BAM
GROUP BY SiteCode
ORDER BY row_count DESC;

SiteCode	row_count	min_date	max_date
B26	27224	2018-05-03	2026-05-15
B35	21297	2019-02-12	2026-05-15
DRD-NOLA	18674	2018-01-15	2026-05-16
B34	15438	2018-05-01	2026-05-15
B25	14646	2018-06-03	2026-05-15
DRD-KVC	12122	2018-05-01	2026-05-15
V12	11840	2018-06-07	2026-05-15
DRD-KVB	11715	2018-04-12	2026-05-15
B33	11477	2018-05-21	2026-05-15
B24	11400	2018-06-18	2026-05-15
V11	9378	2018-06-01	2026-05-15
B41	9136	2018-06-25	2026-05-16
V5	9128	2018-06-01	2026-05-15
B42	9122	2018-04-05	2026-05-15
V9	8239	2018-06-01	2026-05-15
B54	8112	2018-09-27	2026-05-15
DRD-SF	7852	2018-05-01	2026-05-15
B48	7453	2018-05-21	2026-05-15
TTCB	7445	2019-03-01	2026-05-15
V6	7409	2018-06-01	2026-05-15
TTCA	7319	2018-09-27	2026-05-17
B47	6954	2019-09-08	2026-05-15
B45	6891	2018-06-04	2026-05-16
B42A	6798	2018-12-10	2026-05-15
B35A	6303	2018-06-08	2026-05-14
V15	6192	2018-06-07	2026-05-15
V10A	5935	2018-06-01	2026-05-15
B42D	5926	2019-06-27	2026-05-15
VWBY	5869	2022-10-13	2026-05-15
VBRP	5783	2022-10-12	2026-05-16
B38	5409	2018-05-03	2026-05-15
B55	5367	2018-05-10	2026-05-15
LO	5334	2024-02-01	2026-05-16
V10	5194	2018-06-05	2026-05-15
D07	5019	2021-09-01	2026-05-15
V20	4875	2018-05-04	2026-05-15
DRD-CO	4839	2018-05-17	2026-05-15
FW	4797	2024-01-30	2026-05-15
SFN	4786	2018-05-03	2026-05-15
DRD-KC	4472	2018-06-04	2026-05-15
B42C	4430	2019-07-22	2026-05-16
V8	4118	2018-05-14	2026-05-15
V19	4056	2018-05-16	2026-05-15
HNT	4000	2023-10-06	2026-05-15
V21	3773	2018-05-15	2026-05-15
B46	3705	2019-11-11	2026-05-15
HGT	3647	2023-12-14	2026-05-16
V12A	3538	2018-06-01	2026-05-15
V1	3380	2018-06-05	2026-05-15
VMIN	3376	2022-10-07	2026-05-15
V17	3087	2018-06-15	2026-05-15
TTCC	3039	2023-10-10	2026-05-15
B39	3037	2018-06-27	2026-05-15
V14	2937	2018-06-04	2026-05-15
MP	2913	2024-01-10	2026-05-15
B31	2816	2018-06-01	2026-05-15
B44	2788	2018-06-01	2026-05-16
VBRA	2760	2022-10-04	2026-05-15
B29	2639	2018-06-05	2026-05-15
NC	2512	2024-02-15	2026-05-15
B52	2477	2018-06-15	2026-05-14
BG	2435	2024-01-25	2026-05-15
SHP	2310	2023-10-18	2026-05-15
RMD	2248	2024-01-24	2026-05-15
ET	2170	2024-01-29	2026-05-15
B30	2068	2018-06-05	2026-05-15
B36	1958	2019-05-08	2026-05-15
B37	1887	2019-01-07	2026-05-15
LAN	1836	2024-01-09	2026-05-15
B51	1813	2022-12-15	2026-05-15
LV1	1809	2024-02-29	2026-05-15
GAL	1759	2024-01-19	2026-05-15
D08	1739	2022-01-19	2026-05-15
B42B	1723	2019-03-14	2026-05-15
STN	1722	2023-08-17	2026-05-15
B28	1629	2018-07-16	2026-05-15
B12B	1615	2020-05-26	2026-05-15
D09	1511	2022-03-17	2026-05-15
B57D	1422	2023-08-29	2026-05-15
LV2	1305	2024-02-29	2026-05-15
RE	1272	2024-02-22	2026-05-14
B75	1268	2022-12-07	2026-05-14
B27	1236	2022-09-15	2026-05-15
DA	1215	2024-02-14	2026-05-15
WIL	1189	2024-01-22	2026-05-15
DM	1152	2024-02-13	2026-05-14
ELC	1115	2024-01-22	2026-05-15
FR	1102	2024-03-27	2026-05-15
B66A	978	2024-07-16	2026-05-13
B57A	966	2023-09-06	2026-05-15
MNRE	933	2023-10-18	2026-05-15
B57C	922	2023-09-18	2026-05-14
B57	873	2023-09-14	2026-05-15
MRD	843	2023-11-01	2026-05-15
BOI	754	2023-11-03	2026-05-14
CBCO	727	2022-08-10	2026-05-14
LV3	689	2024-02-29	2026-05-15
V5B	593	2023-03-31	2026-05-15
PH	430	2024-02-13	2025-10-10
STVN	419	2023-10-18	2026-05-15
TE	410	2024-02-14	2025-10-14
B57B	367	2023-10-30	2026-05-12
GRC	310	2020-02-17	2022-08-12
AHK	190	2024-06-19	2026-05-11
NLR	124	2025-12-24	2026-05-08
TU	110	2024-02-29	2025-09-16
B76	89	2025-07-28	2026-05-15
TEX	83	2025-12-15	2026-05-15
FS	77	2026-01-06	2026-05-15
BAT	65	2026-01-07	2026-05-15
CON	65	2025-12-15	2026-05-14
FAY	63	2026-01-16	2026-05-13
HS	56	2026-01-14	2026-05-13
JON	34	2025-12-24	2026-05-08
NSL-115	6	2022-11-30	2025-11-12




//Solution that was given by codex....

Yes, I checked it again. The file is populated now, and it has the important pieces we needed. I did not change anything.

The main finding: legacy pats.BAMMerge is not just a simple merge from FormQuestionAnswers. It depends on a view, pats.vw_BAM, and that view combines three BAM sources.

From BamQueriesandSolutions.md (line 1):

pats.vw_BAM =
  branch 1: pats.tbl_BriefAddictionMonitor
  branch 2: pats.tbl_dbo_FormQuestionAnswers
  branch 3: pats.tbl_BAMForm
So for Fabric, we have two choices.

Choice 1: FormQuestionAnswers-Only BAM
This can be implemented inside/after your existing FormQuestionAnswers Fabric flow, but it will only reproduce the cte_BAM*_6 branch from vw_BAM. That means it will calculate BAM rows only from:

bhg_silver.pats.sl_tblFormQuestionAnswers
This is easier, but it will not fully match legacy pats.tbl_vw_BAM.

Choice 2: Full Legacy-Parity BAMMerge
This is the correct version if you want Fabric output to match BHG_DR.

For that, Fabric must also have these tables available:

sl_tblFormQuestionAnswers
sl_tblBriefAddictionMonitor
sl_tblBAMForm
Then create a new notebook after FormQuestionAnswers Silver:

nb_forms_bam_merge
Pipeline order should be:

Execute_AfterBronzFormQuestionAnswer
→ nb_forms_bronze_to_silver
→ nb_forms_bam_merge
The notebook should build the equivalent of pats.vw_BAM, then write to a Fabric target table, preferably Gold because this is derived data:

bhg_gold.pats.gl_tbl_vw_BAM
or if you want naming closer to current Silver:

bhg_silver.pats.sl_tbl_vw_BAM
Legacy target schema from your file is:

Sitecode, cltid, Date, Idx,
ClinicianTEXT, AdminList, PreAdmissionId, fid,
Q1..Q17,
UseCalc, RiskCalc, ProtectiveCalc
Primary key is:

Sitecode + cltid + Date + Idx
That comes from BamQueriesandSolutions.md (line 406).

The important behavior: legacy BAMMerge first does this:

delete from pats.tbl_vw_BAM where SiteCode = @sitecode;
Then it re-inserts/merges fresh rows from pats.vw_BAM.

So in Fabric, do not use only a normal Delta merge, because stale rows may remain. You need either:

delete affected SiteCode rows from target
→ append recalculated rows
or:

overwrite affected SiteCode partitions
The source notebook must also recreate the same Idx logic:

ROW_NUMBER() OVER (
  PARTITION BY SiteCode, fcltid, Date
  ORDER BY SiteCode, fcltid, Date, ClinicianTEXT, AdminList, PreAdmissionId, fid
)
For the FormQuestionAnswers branch, the key mappings are now clear:

AdminMethod = QuestionId 1720
EvalDate    = QuestionId 1718
StartTime   = QuestionId 1721
EndTime     = QuestionId 1739

Q1  = 1722
Q2  = 1723
Q3  = 1724
Q4  = 1725
Q5  = 1726
Q6  = 1727
Q7a = 1740
Q7b = 1741
Q7c = 1742
Q7d = 1743
Q7e = 1744
Q7f = 1745
Q7g = 1746
Q8  = 1729
Q9  = 1730
Q10 = 1731
Q11 = 1732
Q12 = 1733
Q13 = 1734
Q14 = 1736
Q15 = 1735
Q16 = 1737
Q17 = 1738
Also, the FormQuestionAnswers query shows only one BAM form name:

Brief Addiction Monitor (BAM) With Scoring & Clinical Guidelines
So the Fabric filter can safely start with:

FormName LIKE '%Brief%'
AND RowState = 1
My recommendation: implement nb_forms_bam_merge as a separate post-Silver notebook, and implement the full three-branch vw_BAM logic only if tbl_BriefAddictionMonitor and tbl_BAMForm are already in Fabric. If those two are not in Fabric yet, start with the FormQuestionAnswers branch but clearly label it as partial BAMMerge, not full parity.


BamQueriesandSolutions.md
Document · MD
Open




3:24 PM
so what you are saying is tbl_bam and bamforms are dependecy table by which we canble to calculate these that what you are saying because we havent done this bam and bamform in th fabric yet.. we started with forms migration only as off now..?
3:26 PM


Yes, exactly.

For full legacy BAMMerge parity, these are dependency tables:

pats.tbl_BriefAddictionMonitor
pats.tbl_BAMForm
pats.tbl_dbo_FormQuestionAnswers
Your current Fabric work has only migrated:

bhg_silver.pats.sl_tblFormQuestionAnswers
So right now, Fabric can only calculate the FormQuestionAnswers branch of pats.vw_BAM.

But legacy pats.vw_BAM does this:

old/global BAM source       → pats.tbl_BriefAddictionMonitor
forms-based BAM source      → pats.tbl_dbo_FormQuestionAnswers
new BAM source from Nov 2025 → pats.tbl_BAMForm
Then pats.BAMMerge unions all three and writes into:

pats.tbl_vw_BAM
So if you implement BAMMerge today using only FormQuestionAnswers, your Fabric BAM output will be incomplete compared to BHG_DR.

What you can do now:

Option A: Wait
Finish migrating tbl_BriefAddictionMonitor and tbl_BAMForm, then implement full BAMMerge in Fabric.

Option B: Partial Now
Implement only the FormQuestionAnswers BAM logic now, but name/document it clearly as partial, for example:

nb_forms_bam_merge_from_formqa_only
This will cover rows coming from the BAM form stored inside FormQuestionAnswers, but it will miss rows coming from tbl_BriefAddictionMonitor and tbl_BAMForm.

My recommendation: because you are doing forms migration only right now, do not claim full BAMMerge is complete yet. Add it as a downstream dependency:

Full BAMMerge Fabric implementation depends on:
1. FormQuestionAnswers migration
2. BriefAddictionMonitor migration
3. BAMForm migration
Then once those three Silver tables exist, create one final BAM notebook that recreates pats.vw_BAM and writes the equivalent of pats.tbl_vw_BAM.
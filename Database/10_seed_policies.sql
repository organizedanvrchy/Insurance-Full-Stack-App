USE InsuranceDb;
GO

/* Demo catalog: 4 plans per policy type (20 total).
   MonthlyPremium = BASE monthly rate. The customer's quote multiplies it by their rating factors.
   Safe to re-run: a plan is inserted only if no plan with the same name already exists. */
INSERT INTO dbo.Policies (PolicyName, PolicyType, Description, CoverageAmount, MonthlyPremium, TermMonths)
SELECT v.PolicyName, v.PolicyType, v.Description, v.CoverageAmount, v.MonthlyPremium, v.TermMonths
FROM (VALUES

  /* ---------------- HEALTH (12-month plan years) ---------------- */
  (N'Bronze Health Plan', N'Health',
   N'Lower monthly cost with a higher deductible ($7,500 individual). Preventive care is covered at no charge. Primary care, specialist visits, hospital stays, emergency care and generic prescriptions are covered after the deductible. Best for healthy adults who mainly want protection from major medical costs.',
   100000.00, 320.00, 12),
  (N'Silver Health Plan', N'Health',
   N'A balanced plan with a moderate deductible ($3,500). Includes preventive care, primary care and specialist visits with copays, hospital stays, emergency care, mental health services, and generic and preferred brand prescriptions.',
   250000.00, 450.00, 12),
  (N'Gold Health Plan', N'Health',
   N'Higher monthly cost with a low deductible ($1,000) and predictable copays. Includes preventive care, specialist visits, hospital and surgical care, maternity and newborn care, mental health services and broad prescription coverage.',
   500000.00, 620.00, 12),
  (N'HSA High-Deductible Health Plan', N'Health',
   N'A high-deductible plan ($4,500) designed to be paired with a Health Savings Account. Preventive care is covered before the deductible. Lower premiums make it a good fit if you rarely need care but want financial protection from a serious illness or injury.',
   150000.00, 285.00, 12),

  /* ---------------- AUTO (6- and 12-month terms) ---------------- */
  (N'Auto Basic Liability', N'Auto',
   N'Designed to meet typical minimum legal requirements. Pays for injuries and property damage you cause to others in an accident, plus legal defense costs. Does not cover damage to your own vehicle.',
   50000.00, 75.00, 6),
  (N'Auto Standard Coverage', N'Auto',
   N'Liability protection plus collision coverage for your own vehicle after an accident ($500 deductible) and uninsured motorist coverage. A solid choice for a financed or newer vehicle.',
   100000.00, 140.00, 6),
  (N'Auto Full Coverage', N'Auto',
   N'Higher liability limits, collision, comprehensive (theft, fire, hail, vandalism, animal strikes), uninsured and underinsured motorist coverage, and medical payments. $500 deductible.',
   250000.00, 215.00, 12),
  (N'Auto Premium Plus', N'Auto',
   N'Everything in Full Coverage with the highest liability limits and a $250 deductible. Adds rental car reimbursement, 24/7 roadside assistance, and new car replacement if a vehicle under two years old is totaled.',
   500000.00, 265.00, 12),

  /* ---------------- LIFE (level-premium term policies) ---------------- */
  (N'Term Life 10-Year', N'Life',
   N'Affordable temporary protection. Pays a death benefit to your beneficiaries if you pass away during the 10-year term, with a premium that stays level for the whole term. Often chosen to cover a short-term debt or the early years of raising a family.',
   250000.00, 14.00, 120),
  (N'Term Life 20-Year', N'Life',
   N'A popular middle option. Pays a death benefit to your beneficiaries if you pass away during the 20-year term, with a level premium. Well suited to covering a mortgage and replacing income while children are growing up.',
   500000.00, 29.00, 240),
  (N'Term Life 30-Year', N'Life',
   N'Long-term protection with a level premium for 30 years. A good fit for younger applicants who want coverage that lasts through a full mortgage and most of their working years.',
   500000.00, 38.00, 360),
  (N'Term Life 20-Year Plus', N'Life',
   N'A higher benefit of $1,000,000 for primary earners with dependents and larger obligations. Includes an accelerated death benefit rider that allows early access to part of the benefit in the event of a terminal illness diagnosis.',
   1000000.00, 52.00, 240),

  /* ---------------- HOME (12-month policies) ---------------- */
  (N'Renters Insurance', N'Home',
   N'Covers your personal belongings against theft, fire and water damage. Includes personal liability if someone is injured in your rental, and temporary living expenses if your home becomes uninhabitable. $500 deductible.',
   30000.00, 14.00, 12),
  (N'Condo Insurance', N'Home',
   N'Covers the interior of your condo unit and your personal property. Includes personal liability and loss assessment coverage. Works alongside the master policy carried by your condo association.',
   80000.00, 38.00, 12),
  (N'Homeowners Essential', N'Home',
   N'Covers the structure of your home and other structures such as a detached garage or fence. Also covers personal belongings, personal liability and additional living expenses after a covered loss. $1,500 deductible. Flood and earthquake damage are not included.',
   250000.00, 120.00, 12),
  (N'Homeowners Preferred', N'Home',
   N'Higher dwelling limits with replacement-cost coverage on belongings, water backup coverage, identity theft restoration and higher liability limits. $1,000 deductible. Flood and earthquake damage are not included.',
   500000.00, 210.00, 12),

  /* ---------------- TRAVEL (single trip = 1 month term, annual = 12) ---------------- */
  (N'Single-Trip Basic', N'Travel',
   N'Protection for one trip. Covers emergency medical expenses while traveling, trip cancellation for covered reasons, and lost or delayed baggage. Enter your trip length and destination when you request a quote.',
   10000.00, 55.00, 1),
  (N'Single-Trip Comprehensive', N'Travel',
   N'Everything in Basic with higher limits. Adds trip interruption, missed connection coverage, emergency medical evacuation, rental car damage and 24/7 travel assistance.',
   50000.00, 120.00, 1),
  (N'Annual Multi-Trip Plan', N'Travel',
   N'Covers all your trips for a year (up to 45 days each) with one policy. Includes emergency medical, trip cancellation and interruption, and baggage protection. Good value if you travel more than twice a year.',
   100000.00, 28.00, 12),
  (N'Annual Multi-Trip Premium', N'Travel',
   N'The most complete annual plan. Higher medical limits, emergency medical evacuation and repatriation, coverage for adventure activities, and an option to cancel for reasons beyond the standard covered list.',
   250000.00, 55.00, 12)

) AS v (PolicyName, PolicyType, Description, CoverageAmount, MonthlyPremium, TermMonths)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Policies p WHERE p.PolicyName = v.PolicyName);

-- Check the result
SELECT PolicyType, PolicyName, CoverageAmount, MonthlyPremium AS BaseMonthly, TermMonths, IsActive
FROM dbo.Policies
ORDER BY PolicyType, MonthlyPremium;
GO
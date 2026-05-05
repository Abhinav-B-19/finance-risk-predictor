public static class FeatureBuilder
{
    public static Dictionary<string, object> Build(
        List<Prediction> history,
        PredictionRequestDto request)
    {
        double dti = request.Debt / request.Income;

        double GetDti(Prediction p) =>
            p.Income > 0 ? p.Debt / p.Income : 0;

        var prev1 = history.ElementAtOrDefault(0);
        var prev2 = history.ElementAtOrDefault(1);

        var prev1Dti = prev1 != null ? GetDti(prev1) : dti;
        var prev2Dti = prev2 != null ? GetDti(prev2) : prev1Dti;

        var prev1Savings = prev1 != null
            ? (prev1.Income - prev1.Expenses) / prev1.Income
            : 0;

        var prev2Savings = prev2 != null
            ? (prev2.Income - prev2.Expenses) / prev2.Income
            : 0;

        return new Dictionary<string, object>
        {
            ["income"] = request.Income,
            ["expenses"] = request.Expenses,
            ["debt"] = request.Debt,
            ["shock"] = "none",

            ["dti_lag1"] = prev1Dti,
            ["dti_lag2"] = prev2Dti,
            ["savings_ratio_lag1"] = prev1Savings,
            ["savings_ratio_lag2"] = prev2Savings,
            ["debt_lag1"] = prev1?.Debt ?? request.Debt,
            ["debt_lag2"] = prev2?.Debt ?? request.Debt
        };
    }
}
namespace Flit.Procedures.Runtime.Dashboard;

public static class ProcedureCategoryClassifier
{
    public const string Matriculas = "matriculas";
    public const string Traspasos = "traspasos";
    public const string Otros = "otros";

    public static string Classify(string procedureTypeCode)
    {
        if (procedureTypeCode.StartsWith("MATRICULA", StringComparison.OrdinalIgnoreCase))
        {
            return Matriculas;
        }

        if (string.Equals(procedureTypeCode, "TRASPASO", StringComparison.OrdinalIgnoreCase))
        {
            return Traspasos;
        }

        return Otros;
    }

    public static bool MatchesCategory(string procedureTypeCode, string category) =>
        string.Equals(Classify(procedureTypeCode), category, StringComparison.OrdinalIgnoreCase);
}

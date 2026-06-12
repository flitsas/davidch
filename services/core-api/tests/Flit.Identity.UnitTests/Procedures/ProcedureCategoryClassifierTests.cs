using Flit.Procedures.Runtime.Dashboard;

namespace Flit.Identity.UnitTests.Procedures;

public class ProcedureCategoryClassifierTests
{
    [Theory]
    [InlineData("MATRICULA_INICIAL", "matriculas")]
    [InlineData("matricula_extra", "matriculas")]
    [InlineData("TRASPASO", "traspasos")]
    [InlineData("RADICADO_CUENTA", "otros")]
    public void Classify_maps_codes(string code, string expected) =>
        Assert.Equal(expected, ProcedureCategoryClassifier.Classify(code));
}

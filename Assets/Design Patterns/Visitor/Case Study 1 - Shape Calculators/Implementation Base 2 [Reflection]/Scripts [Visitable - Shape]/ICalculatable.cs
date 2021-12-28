namespace VisitorPattern.Case1.Base2 {
  /// <summary>
  /// * [The 'Visitable Element Contract']
  /// </summary>
  public interface ICalculatable {
    /// <summary>
    /// * [The 'Accept()' method]
    /// </summary>
    double ProcessCalculation(Calculator calculator);
  }
}
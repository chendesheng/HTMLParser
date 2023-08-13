class HTMLAttribute {
  public HTMLAttribute(string name, string value) {
    this.name = new StringBuilder(name);
    this.value = new StringBuilder(value);
  }

  public StringBuilder name { get; set; }
  public StringBuilder value { get; set; }
  public override string ToString() {
    return $"{{name: '{name}', value: '{value}'}}";
  }
}

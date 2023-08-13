class Element : Node {
  public Element(Document document, string? ns, string name) : base(document) {
    namespace_uri = ns;
    tag_name = name;
    _custom_element_state = CustomElementState.Undefined;
  }

  public string? namespace_uri { get; private set; }
  public string tag_name { get; private set; }

  List<Attr> _attributes = new();
  string? _namespace_prefix;
  CustomElementState _custom_element_state;


  public HTMLCollection get_elements_by_tag_name(string qualifiedName) {
    throw new NotImplementedException();
  }

  public void set_attribute(string qualified_name, string value) { }
  public string get_attribute(string qualified_name) { return ""; }
  public void set_attribute_node(Attr attr) { }
  public Attr? get_attribute_node(string qualified_name) { return _attributes.Find(attr => attr.name == qualified_name); }
  public bool toggle_attribute(string qualified_name) { return false; }
  public bool has_attribute(string qualified_name) { return false; }

  public void set_node_document_to_attributes(Document? document) {
    foreach (var attr in _attributes) {
      attr.node_document = document;
    }
  }

  // https://dom.spec.whatwg.org/#concept-element-attributes-append
  public void append_attribute(IEnumerable<HTMLAttribute> attrs) {
    foreach (var attr in attrs) {
      _attributes.Add(new Attr(node_document, attr.name.ToString(), attr.value.ToString(), this));
    }
  }

  // https://dom.spec.whatwg.org/#concept-element-custom-element-state
  enum CustomElementState {
    Undefined,
    Failed,
    Uncustomized,
    Precustomized,
    Custom,
  }

  // protected override String display_name {
  //   get {
  //     return tag_name;
  //   }
  // }
}

class Attr : Node {
  public Attr(Document document, string name, string value, Element? owner_element) : base(document) {
    this.name = name;
    this.value = value;
    this.owner_element = owner_element;
  }

  public string name { get; private set; }
  public string value { get; private set; }
  public Element? owner_element { get; private set; }
}

class NodeList {
  public NodeList(IEnumerable<Node> nodes) {
    _nodes = new(nodes);
  }
  public Node? item(ulong i) { return _nodes[(int)i]; }
  public ulong length { get { return (ulong)_nodes.Count; } }

  protected List<Node> _nodes;
}

class HTMLCollection : NodeList {
  public HTMLCollection(IEnumerable<Element> elements): base(elements.Cast<Node>()) {
  }

  public Element? named_item(string name) {
    return this._nodes.Find(node => ((Element)node).node_name == name) as Element;
  }
}

class DOMException : Exception {
}

class NotFoundError : DOMException { }
class HierarchyRequestError : DOMException { }

class Attr : Node {
  public string name { get; private set; }
  public string value { get; private set; }
  public Element? owner_element { get { return null; } }
}

// https://dom.spec.whatwg.org/#interface-characterdata
class CharacterData : Node {
  public string data { get; set; }
  public ulong length { get; }
  public string substring_data(ulong offset, ulong count) { return ""; }
  public string append_data(string data) { return ""; }
  public string insert_data(ulong offset, string data) { return ""; }
  public string delete_data(ulong offset, ulong count) { return ""; }
  public string replace_data(ulong offset, ulong count, string data) { return ""; }
}

// https://dom.spec.whatwg.org/#interface-comment 
class Comment : CharacterData { }

// https://dom.spec.whatwg.org/#interface-processinginstruction
class ProcessingInstruction : CharacterData { }

class Text : CharacterData {
  public Text(string data = "") { }
  public Text split_text(ulong offset) {
    return new Text();
  }
  public string whole_text { get { return ""; } }
}

class Element : Node {
  public string? namespace_uri { get { return null; } }
  public string tag_name { get; private set; }

  public HTMLCollection get_elements_by_tag_name(string qualifiedName) {
    throw new NotImplementedException();
  }

  public void set_attribute(string qualified_name, string value) { }
  public string get_attribute(string qualified_name) { return ""; }
  public void set_attribute_node(Attr attr) { }
  public Attr get_attribute_node(string qualified_name) { return new Attr(); }
  public bool toggle_attribute(string qualified_name) { return false; }
  public bool has_attribute(string qualified_name) { return false; }

  internal void set_node_document_to_attributes(Document? document) {
    foreach (var attr in _attributes) {
      attr.owner_document = document;
    }
  }

  List<Attr> _attributes = new();
}

interface HTMLOrSVGElement {
  long tab_index { get; set; }
  bool autofocus { get; set; }
}

class HTMLElement : Element, HTMLOrSVGElement {
  public string title { get; set; }
  public string lang { get; set; }

  public bool? hidden { get; set; }
  public string inner_text { get; set; }
  public string outer_text { get; set; }
  public long tab_index { get; set; }
  public bool autofocus { get; set; }
}

class HTMLHeadElement : HTMLElement {
}

class DocumentFragment : Node { }

class DocumentType : Node {
  public DocumentType(string name, string public_id, string system_id) {
    this.name = name;
    this.public_id = public_id;
    this.system_id = system_id;
  }

  public string name { get; private set; } 
  public string public_id { get; private set; } 
  public string system_id { get; private set; } 
};

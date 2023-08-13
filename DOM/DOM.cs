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


// https://dom.spec.whatwg.org/#interface-characterdata
class CharacterData : Node {
  public CharacterData(Document document) : base(document) {}
  public string data { get; set; }
  public ulong length { get; }
  public string substring_data(ulong offset, ulong count) { return ""; }
  public string append_data(string data) { return ""; }
  public string insert_data(ulong offset, string data) { return ""; }
  public string delete_data(ulong offset, ulong count) { return ""; }
  public string replace_data(ulong offset, ulong count, string data) { return ""; }
}

// https://dom.spec.whatwg.org/#interface-comment 
class Comment : CharacterData {
  public Comment(Document document) : base(document) {}
}

// https://dom.spec.whatwg.org/#interface-processinginstruction
class ProcessingInstruction : CharacterData {
  public ProcessingInstruction(Document document) : base(document) {}
}

class Text : CharacterData {
  public Text(Document document, string data = "") : base(document) { }
  public Text split_text(ulong offset) {
    return new Text(node_document);
  }
  public string whole_text { get { return ""; } }
}

interface HTMLOrSVGElement {
  long tab_index { get; set; }
  bool autofocus { get; set; }
}

class DocumentFragment : Node {
  public DocumentFragment(Document document) : base(document) {}
}

class DocumentType : Node {
  public DocumentType(Document document, string name, string public_id, string system_id) : base(document) {
    this.name = name;
    this.public_id = public_id;
    this.system_id = system_id;
  }

  public string name { get; private set; } 
  public string public_id { get; private set; } 
  public string system_id { get; private set; } 
};

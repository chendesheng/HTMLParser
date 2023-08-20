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
class IndexSizeError : DOMException {}


// https://dom.spec.whatwg.org/#interface-characterdata
class CharacterData : Node {
  public CharacterData(Document document, string data = "") : base(document) {
    _data = data;
  }
  string _data;

  // The data getter steps are to return this’s data. Its setter must replace data with node this, offset 0, count this’s length, and data new value.
  public string data {
    get { return _data; }
    set { replace_data(0, length, value); }
  }
  public ulong length { get { return (ulong)_data.Length; } }

  // https://dom.spec.whatwg.org/#concept-cd-substring
  public string substring_data(ulong offset, ulong count) {
    // 1. Let length be node’s length.
    var length = this.length;
    // 2. If offset is greater than length, then throw an "IndexSizeError" DOMException.
    if (offset > length) {
      throw new IndexSizeError();
    }
    // 3. If offset plus count is greater than length, return a string whose value is the code units from the offset_th code unit to the end of node’s data, and then return.
    if (offset + count > length) {
      return _data.Substring((int)offset);
    }
    // 4. Return a string whose value is the code units from the offsetth code unit to the offset+countth code unit in node’s data.
    return _data.Substring((int)offset, (int)count);
  }
  // The appendData(data) method steps are to replace data with node this, offset this’s length, count 0, and data data.
  public void append_data(string data) {
    replace_data(length, 0, data);
  }

  // The insertData(offset, data) method steps are to replace data with node this, offset offset, count 0, and data data.
  public void insert_data(ulong offset, string data) {
    replace_data(offset, 0, data);
  }

  // The deleteData(offset, count) method steps are to replace data with node this, offset offset, count count, and data the empty string.
  public void delete_data(ulong offset, ulong count) {
    replace_data(offset, count, "");
  }

  // https://dom.spec.whatwg.org/#concept-cd-replace
  public void replace_data(ulong offset, ulong count, string data) {
    // 1. Let length be node’s length.
    var length = this.length;
    // 2. If offset is greater than length, then throw an "IndexSizeError" DOMException.
    if (offset > length) {
      throw new IndexSizeError();
    }
    // 3. If offset plus count is greater than length, then set count to length minus offset.
    if (offset + count > length) {
      count = length - offset;
    }
    // 4. Queue a mutation record of "characterData" for node with null, null, node’s data, « », « », null, and null.
    // FIXME: mutation record

    // 5. Insert data into node’s data after offset code units.
    _data = _data.Insert((int)offset, data);
    // 6. Let delete offset be offset + data’s length.
    var delete_offset = offset + (ulong)data.Length;
    // 7. Starting from delete offset code units, remove count code units from node’s data.
    _data = _data.Remove((int)delete_offset, (int)count);
    // 8. For each live range whose start node is node and start offset is greater than offset but less than or equal to offset plus count, set its start offset to offset.
    // FIXME: live range

    // 9. For each live range whose end node is node and end offset is greater than offset but less than or equal to offset plus count, set its end offset to offset.
    // FIXME: live range

    // 10. For each live range whose start node is node and start offset is greater than offset plus count, increase its start offset by data’s length and decrease it by count.
    // FIXME: live range

    // 11. For each live range whose end node is node and end offset is greater than offset plus count, increase its end offset by data’s length and decrease it by count.
    // FIXME: live range

    // 12. If node’s parent is non-null, then run the children changed steps for node’s parent.
    // FIXME: children changed steps
  }

  public override string ToString() {
    return $"[{_data}]";
  }
}

// https://dom.spec.whatwg.org/#interface-comment 
class Comment : CharacterData {
  public Comment(Document document) : base(document) {}
}

// https://dom.spec.whatwg.org/#interface-processinginstruction
class ProcessingInstruction : CharacterData {
  public ProcessingInstruction(Document document) : base(document) {}
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

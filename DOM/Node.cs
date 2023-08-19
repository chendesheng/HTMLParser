global using DOMString = System.String;

class Node : EventTarget {
  public const ushort ELEMENT_NODE = 1;
  public const ushort ATTRIBUTE_NODE = 2;
  public const ushort TEXT_NODE = 3;
  public const ushort CDATA_SECTION_NODE = 4;
  public const ushort ENTITY_REFERENCE_NODE = 5; // legacy
  public const ushort ENTITY_NODE = 6; // legacy
  public const ushort PROCESSING_INSTRUCTION_NODE = 7;
  public const ushort COMMENT_NODE = 8;
  public const ushort DOCUMENT_NODE = 9;
  public const ushort DOCUMENT_TYPE_NODE = 10;
  public const ushort DOCUMENT_FRAGMENT_NODE = 11;
  public const ushort NOTATION_NODE = 12; // legacy

  public Node(Document node_document) {
    this.node_document = node_document;
  }

  public ushort node_type { get; private set; }
  public string node_name { get; set; }

  public bool is_connected { get { return false; } }
  public Document node_document { get; set; }
  // https://dom.spec.whatwg.org/#dom-node-ownerdocument
  public Document? owner_document {
    get {
      if (this is Document) return null;
      return node_document;
    }
  }

  private Node? _parent = null;
  public Node? parent_node { get { return _parent; } }
  public Element? parent_element { get { return _parent as Element; } }

  public bool has_child_nodes => _children.Count > 0;

  public NodeList child_nodes { get { return new NodeList(_children); } }

  public Node? first_child {
    get {
      if (has_child_nodes) return _children[0];
      return null;
    }
  }
  public Node? last_child {
    get {
      if (has_child_nodes) return _children[_children.Count - 1];
      return null;
    }
  }
  public Node? previous_sibling {
    get {
      if (parent_node == null) return null;
      var i = index - 1;
      if (i >= 0) return parent_node._children[i];
      return null;
    }
  }
  public Node? next_sibling {
    get {
      if (parent_node == null) return null;
      var i = index + 1;
      if (i < parent_node._children.Count) return parent_node._children[i];
      return null;
    }
  }

  public string? node_value { get { return null; } }
  public string? text_content { get { return null; } }

  public string child_text_content() {
    var sb = new StringBuilder();
    foreach (var child in _children) {
      if (child is Text text) {
        sb.Append(text.data);
      } else {
        sb.Append(child.child_text_content());
      }
    }
    return sb.ToString();
  }

  public bool dispatch_event(Event e) {
    return false;
  }
  public void add_event_listener(string type, EventHandler? callback = null) { }
  public void remove_event_listener(string type, EventHandler? callback = null) { }



  public Node insert_before(Node node, Node? child) {
    // The insertBefore(node, child) method steps are to return the result of pre-inserting node into this before child.
    return pre_insert(node, this, child);
  }

  // https://dom.spec.whatwg.org/#dom-node-appendchild
  public Node append_child(Node node) {
    // The appendChild(node) method steps are to return the result of appending node to this.
    // To append a node to a parent, pre-insert node into parent before null.
    return pre_insert(node, this, null);
  }

  // The replaceChild(node, child) method steps are to return the result of replacing child with node within this.
  public Node replace_child(Node node, Node child) {
    return repalce_child(child, node, this);
  }

  // The removeChild(child) method steps are to return the result of pre-removing child from this.
  public Node remove_child(Node child) {
    return pre_remove(child, this);
  }

  // https://dom.spec.whatwg.org/#concept-node-replace
  private static Node repalce_child(Node child, Node node, Node parent) {
    // To replace a child with node within a parent, run these steps:
    // 1. If parent is not a Document, DocumentFragment, or Element node, then throw a "HierarchyRequestError" DOMException.
    if (parent is not Document && parent is not DocumentFragment && parent is not Element) {
      throw new HierarchyRequestError();
    }

    // 2. If node is a host-including inclusive ancestor of parent, then throw a "HierarchyRequestError" DOMException.
    // FIXME

    // 3. If child’s parent is not parent, then throw a "NotFoundError" DOMException.
    if (child.parent_node != parent) throw new NotFoundError();

    // 4. If node is not a DocumentFragment, DocumentType, Element, or CharacterData node, then throw a "HierarchyRequestError" DOMException.
    if (node is not DocumentFragment && node is not DocumentType && node is not Element && node is not CharacterData) {
      throw new HierarchyRequestError();
    }

    // 5. If either node is a Text node and parent is a document, or node is a doctype and parent is not a document, then throw a "HierarchyRequestError" DOMException.
    if (node is Text && parent is Document || node is DocumentType && parent is not Document) {
      throw new HierarchyRequestError();
    }

    // 6. If parent is a document, and any of the statements below, switched on the interface node implements, are true, then throw a "HierarchyRequestError" DOMException.
    if (parent is Document) {
      if (node is DocumentFragment) {
        // If node has more than one element child or has a Text node child.
        if (node._children.Count >= 1 || child.has_text_node_child) {
          throw new HierarchyRequestError();
        } else if (node._children.Count == 1 &&
            (parent.has_element_child && parent.find_element_child() != child || child.find_following_node() is DocumentType)) {
          // Otherwise, if node has one element child and either parent has an element child that is not child or a doctype is following child.
          throw new HierarchyRequestError();
        }
      } else if (node is Element) {
        if (parent.has_element_child && parent.find_element_child() != child || child.find_following_node() is DocumentType) {
          throw new HierarchyRequestError();
        }
      } else if (node is DocumentType) {
        if (parent.has_doctype_child && parent.find_doctype_child() != child || child.find_preceding_node() is Element) {
          throw new HierarchyRequestError();
        }
      }
    }

    // 7. Let referenceChild be child’s next sibling.
    var reference_child = child.next_sibling;

    // 8. If referenceChild is node, then set referenceChild to node’s next sibling.
    if (reference_child == node) {
      reference_child = node.next_sibling;
    }

    // 9. Let previousSibling be child’s previous sibling.
    // FIXME
    // var previousSibling = child.previousSibling;

    // 10. Let removedNodes be the empty set.
    var removed_nodes = new List<Node>();
    // 11. If child’s parent is non-null, then:
    if (child.parent_node != null) {
      // Set removedNodes to « child ».
      removed_nodes.Add(child);

      // Remove child with the suppress observers flag set.
      remove_node(child);
    }

    // 12. Let nodes be node’s children if node is a DocumentFragment node; otherwise « node ».
    // FIXME
    // var nodes = node is DocumentFragment ? node._children : new List<Node>{ node };

    // 13. Insert node into parent before referenceChild with the suppress observers flag set.
    insert_node_into_parent_before_child(node, parent, reference_child);

    // 14. Queue a tree mutation record for parent with nodes, removedNodes, previousSibling, and referenceChild.
    // FIXME

    // Return child.
    return child;
  }

  private Node? find_preceding_node() {
    if (previous_sibling != null) return previous_sibling;
    if (parent_node != null) return parent_node;
    return null;
  }

  private static Node pre_remove(Node child, Node parent) {
    // 1. If child’s parent is not parent, then throw a "NotFoundError" DOMException.
    if (child.parent_node != parent) throw new NotFoundError();

    // 2. remove child
    remove_node(child);

    // 3. return child
    return child;
  }

  // https://dom.spec.whatwg.org/#concept-node-remove
  private static void remove_node(Node node) {
    // To remove a node node, with an optional suppress observers flag, run these steps:
    // 1. Let parent be node’s parent.
    var parent = node.parent_node;
    // 2. Assert: parent is non-null.
    Debug.Assert(parent != null);

    // 3. Let index be node’s index.
    // FIXME

    // 4. For each live range whose start node is an inclusive descendant of node, set its start to (parent, index).
    // FIXME

    // 5. For each live range whose end node is an inclusive descendant of node, set its end to (parent, index).
    // FIXME

    // 6. For each live range whose start node is parent and start offset is greater than index, decrease its start offset by 1.
    // FIXME

    // 7. For each live range whose end node is parent and end offset is greater than index, decrease its end offset by 1.
    // FIXME

    // 8. For each NodeIterator object iterator whose root’s node document is node’s node document, run the NodeIterator pre-removing steps given node and iterator.
    // FIXME

    // 9. Let oldPreviousSibling be node’s previous sibling.
    // FIXME

    // 10. Let oldNextSibling be node’s next sibling.
    // FIXME

    // 11. Remove node from its parent’s children.
    parent._children.Remove(node);

    // 12. If node is assigned, then run assign slottables for node’s assigned slot.
    // FIXME

    // 13. If parent’s root is a shadow root, and parent is a slot whose assigned nodes is the empty list, then run signal a slot change for parent.
    // FIXME

    // 14. If node has an inclusive descendant that is a slot, then:
    // FIXME

    // 14.1. Run assign slottables for a tree with parent’s root.
    // FIXME

    // 14.2. Run assign slottables for a tree with node.
    // FIXME

    // 15. Run the removing steps with node and parent.
    // FIXME

    // 16. Let isParentConnected be parent’s connected.
    // FIXME

    // 17. If node is custom and isParentConnected is true, then enqueue a custom element callback reaction with node, callback name "disconnectedCallback", and an empty argument list.
    // FIXME

    // 18. For each shadow-including descendant descendant of node, in shadow-including tree order, then:
    // 18.1. Run the removing steps with descendant and null.
    // 18.2. If descendant is custom and isParentConnected is true, then enqueue a custom element callback reaction with descendant, callback name "disconnectedCallback", and an empty argument list.

    // 19. For each inclusive ancestor inclusiveAncestor of parent, and then for each registered of inclusiveAncestor’s registered observer list, if registered’s options["subtree"] is true, then append a new transient registered observer whose observer is registered’s observer, options is registered’s options, and source is registered to node’s registered observer list.

    // 20. If suppress observers flag is unset, then queue a tree mutation record for parent with « », « node », oldPreviousSibling, and oldNextSibling.
    // FIXME

    // 21. Run the children changed steps for parent.
    // FIXME
  }

  // https://dom.spec.whatwg.org/#concept-node-pre-insert
  static Node pre_insert(Node node, Node parent, Node? child) {
    // 1. Ensure pre-insertion validity of node into parent before child.
    ensure_pre_insert_validity(node, parent, child);
    // 2. Let referenceChild be child.
    var reference_child = child;
    // 3. If referenceChild is node, then set referenceChild to node’s next sibling.
    if (reference_child == node) {
      reference_child = node.next_sibling;
    }
    // 4. Insert node into parent before referenceChild.
    insert_node_into_parent_before_child(node, parent, reference_child);
    // 5. Return node.
    return node;
  }

  // https://dom.spec.whatwg.org/#concept-node-insert
  private static void insert_node_into_parent_before_child(Node node, Node parent, Node? child) {
    Console.WriteLine($"insert_node_into_parent_before_child({node}, {parent}, {child}");
    // To insert a node into a parent before a child, with an optional suppress observers flag, run these steps:
    // FIXME: add optional suppress observers flag

    // 1. Let nodes be node’s children, if node is a DocumentFragment node; otherwise « node ».
    var nodes = node is DocumentFragment ? node._children : new List<Node> { node };
    // 2. Let count be nodes’s size.
    var count = nodes.Count;
    // 3. If count is 0, then return.
    if (count == 0) return;
    // 4. If node is a DocumentFragment node, then:
    if (node is DocumentFragment) {
      // 1. Remove its children with the suppress observers flag set.
      // 2. Queue a tree mutation record for node with « », nodes, null, and null.
      throw new NotImplementedException();
    }
    // 5. If child is non-null, then:
    if (child != null) {
      // FIXME
      // 1. For each live range whose start node is parent and start offset is greater than child’s index, increase its start offset by count.
      // 2. For each live range whose end node is parent and end offset is greater than child’s index, increase its end offset by count.
    }

    // 6. Let previousSibling be child’s previous sibling or parent’s last child if child is null.
    // FIXME
    // var previousSibling = child != null ? child.previousSibling : parent.lastChild;

    // 7. For each node in nodes, in tree order:
    foreach (var nd in nodes) {
      // 1. Adopt node into parent’s node document.
      adopt_node_into_document(nd, parent.node_document);
      // 2. If child is null, then append node to parent’s children.
      if (child == null) {
        Console.WriteLine($"append_ordered_set({nd}, {parent._children}");
        append_ordered_set(nd, parent._children);
        Console.WriteLine($"{parent}.children: {parent._children.Count}");
      } else {
        // 3. Otherwise, insert node into parent’s children before child’s index.
        parent._children.Insert(child.index, nd);
      }

      // 4. If parent is a shadow host whose shadow root’s slot assignment is "named" and node is a slottable, then assign a slot for node.
      // FIXME

      // 5. If parent’s root is a shadow root, and parent is a slot whose assigned nodes is the empty list, then run signal a slot change for parent.
      // FIXME

      // 6. Run assign slottables for a tree with node’s root.
      // FIXME

      // 7. For each shadow-including inclusive descendant inclusiveDescendant of node, in shadow-including tree order:
      // FIXME
    }
    // 8. If suppress observers flag is unset, then queue a tree mutation record for parent with nodes, « », previousSibling, and child.
    // FIXME

    // 9. Run the children changed steps for parent.
    // FIXME
  }

  // https://infra.spec.whatwg.org/#set-append
  private static void append_ordered_set(Node node, List<Node> children) {
    // To append to an ordered set: if the set contains the given item, then do nothing; otherwise, perform the normal list append operation.
    if (children.Contains(node)) return;
    children.Add(node);
  }

  // https://dom.spec.whatwg.org/#concept-node-adopt
  private static void adopt_node_into_document(Node node, Document? document) {
    // 1. Let oldDocument be node’s node document.
    var old_document = node.node_document;
    // 2. If node’s parent is non-null, then remove node.
    if (node.parent_node != null) remove_node(node);
    if (document != old_document) {
      // For each inclusiveDescendant in node’s shadow-including inclusive descendants:
      foreach (var descendant in node.shadow_including_inclusive_descendants()) {
        // Set inclusiveDescendant’s node document to document.
        descendant.node_document = document;

        // If inclusiveDescendant is an element, then set the node document of each attribute in inclusiveDescendant’s attribute list to document.
        if (descendant is Element ele) {
          // FIXME: override ownDocument??
          ele.set_node_document_to_attributes(document);
        }
      }
    }
  }

  // FIXME: An object A is a shadow-including descendant of an object B, if A is a descendant of B, or A’s root is a shadow root and A’s root’s host is a shadow-including inclusive descendant of B.
  private IEnumerable<Node> shadow_including_inclusive_descendants() {
    yield return this;
    foreach (var c in this._children.SelectMany((c) => c.shadow_including_inclusive_descendants())) {
      yield return c;
    }
  }

  // https://dom.spec.whatwg.org/#concept-node-ensure-pre-insertion-validity
  private static void ensure_pre_insert_validity(Node node, Node parent, Node? child) {
    // 1. If parent is not a Document, DocumentFragment, or Element node, then throw a "HierarchyRequestError" DOMException.
    if (parent is not Document && parent is not DocumentFragment && parent is not Element) {
      throw new HierarchyRequestError();
    }

    // 2. If node is a host-including inclusive ancestor of parent, then throw a "HierarchyRequestError" DOMException.
    if (node.is_host_including_inclusive_ancestor(parent)) {
      throw new HierarchyRequestError();
    }

    // 3. If child is non-null and its parent is not parent, then throw a "NotFoundError" DOMException.
    if (child != null && child.parent_node != parent) throw new NotFoundError();

    // 4. If node is not a DocumentFragment, DocumentType, Element, or CharacterData node, then throw a "HierarchyRequestError" DOMException.
    if (node is not DocumentFragment && node is not DocumentType && node is not Element && node is not CharacterData) {
      throw new HierarchyRequestError();
    }

    // 5. If either node is a Text node and parent is a document, or node is a doctype and parent is not a document, then throw a "HierarchyRequestError" DOMException.
    if (node is Text && node.parent_node is Document || node is DocumentType && node.parent_node is not Document) {
      throw new HierarchyRequestError();
    }

    // 6. If parent is a document, and any of the statements below, switched on the interface node implements, are true, then throw a "HierarchyRequestError" DOMException.
    if (parent is Document) {
      if (node is DocumentFragment) {
        // If node has more than one element child or has a Text node child.
        // Otherwise, if node has one element child and either parent has an element child, child is a doctype, or child is non-null and a doctype is following child.
        if (node.child_nodes.length > 1 || node.has_text_node_child) {
          throw new HierarchyRequestError();
        } else if (node.child_nodes.length == 1 && (parent.has_element_child || child is DocumentType || child?.find_following_node() is DocumentType)) {
          throw new HierarchyRequestError();
        }
      }
    }
  }

  private Node? find_following_node() {
    // An object A is following an object B if A and B are in the same tree and A comes after B in tree order.
    // In tree order is preorder, depth-first traversal of a tree.
    if (first_child != null) return first_child;

    var current = this;
    while (current != null) {
      if (current.next_sibling != null) return current.next_sibling;
      current = current.parent_node;
    }
    return null;
  }

  public Node find_root() {
    // The root of an object is itself, if its parent is null, or else it is the root of its parent. 
    if (this.parent_node == null) return this;
    else return this.parent_node.find_root();
  }

  private Element? find_element_child() {
    return _children.Find(node => node is Element) as Element;
  }
  private DocumentType? find_doctype_child() {
    return _children.Find(node => node is DocumentType) as DocumentType;
  }
  private bool has_element_child => _children.Exists(node => node is Element);
  private bool has_text_node_child => _children.Exists(node => node is Text);

  // https://dom.spec.whatwg.org/#concept-tree-host-including-inclusive-ancestor
  private bool is_host_including_inclusive_ancestor(Node parent) {
    // An object A is a host-including inclusive ancestor of an object B, if either A is an inclusive ancestor of B, or if B’s root has a non-null host and A is a host-including inclusive ancestor of B’s root’s host.
    return false;
  }

  // https://dom.spec.whatwg.org/#concept-tree-inclusive-ancestor
  private bool is_inclusive_ancestor(Node node) {
    // An inclusive ancestor is an object or one of its ancestors.
    var current = this;
    while (current != null) {
      if (current == node) return true;
      current = current.parent_node;
    }
    return false;
  }

  List<Node> _children = new List<Node>();
  bool has_doctype_child {
    get {
      return _children.Exists(node => node is DocumentType);
    }
  }

  private int index {
    get {
      Debug.Assert(parent_node != null);

      return parent_node!._children.IndexOf(this);
    }
  }

  public bool is_shadow_root {
    get {
      return false;
    }
  }

  protected virtual String display_name {
    get {
      return this.GetType().ToString();
    }
  } 

  protected String to_string(int indent) {
    var sb = new StringBuilder();
    for (var i = 0; i < indent * 2; ++i) sb.Append(' '); 
    sb.AppendLine($"[{display_name}]");
    if (has_child_nodes) {
      Console.WriteLine($"to_string: {_children}");
      foreach(var child in _children) {
        sb.Append(child.to_string(indent + 1));
      }
    }
    return sb.ToString();
  }

  public override String ToString() {
    return to_string(0);
  }
}

class HTMLToken {
  public HTMLToken(Type type) {
    _type = type;
    _doctype = type == Type.DOCTYPE ? new DocType() : null;
    _tag = (type == Type.StartTag || type == Type.EndTag) ? new Tag() : null;
    _comment_or_character = (type == Type.Character || type == Type.Comment) ? new Data() : null;
  }
  readonly Type _type;
  readonly DocType? _doctype;
  readonly Tag? _tag;
  readonly Data? _comment_or_character;

  public bool is_start_tag_of(params string[] names) {
    return _type == Type.StartTag && names.Contains(tag!.name);
  }

  public bool is_end_tag_of(params string[] names) {
    return _type == Type.EndTag && names.Contains(tag!.name);
  }

  public bool is_end_tag {
    get { return _type == Type.EndTag; }
  }

  public bool is_start_tag {
    get { return _type == Type.StartTag; }
  }

  // A character token that is one of U+0009 CHARACTER TABULATION, U+000A LINE FEED (LF), U+000C FORM FEED (FF), U+000D CARRIAGE RETURN (CR), or U+0020 SPACE
  public bool is_space_character {
    get {
      if (_type == Type.Character) {
        var c = _comment_or_character!.first_character;
        return c == 0x0009 || c == 0x000A || c == 0x000C || c == 0x000D || c == 0x0020;
      }
      return false;
    }
  }

  public bool is_comment { get { return _type == Type.Comment; } }
  public bool is_doctype { get { return _type == Type.DOCTYPE; } }

  public enum Type {
    DOCTYPE,
    StartTag,
    EndTag,
    Comment,
    Character,
    EndOfFile,
  }

  public class DocType {
    StringBuilder _name = new StringBuilder();
    StringBuilder? _public_identifier;
    StringBuilder? _system_identifier;
    public string name { get { return _name.ToString(); } }
    public string? public_identifier { get { return _public_identifier?.ToString(); } }
    public string? system_identifier { get { return _system_identifier?.ToString(); } }
    public bool force_quirks = false;
    public override string ToString() {
      return $"{{name: '{name}', public_identifier: '{_public_identifier?.ToString() ?? "null"}', system_public_identifier: '{_system_identifier?.ToString() ?? "null"}', force_quirks: {force_quirks}}}";
    }
    public void append_to_name(int codepoint) {
      _name.Append((char)codepoint);
    }
    public void append_to_public_identifier(int codepoint) {
      Debug.Assert(_public_identifier != null);
      _public_identifier.Append((char)codepoint);
    }
    public void append_to_system_identifier(int codepoint) {
      Debug.Assert(_system_identifier != null);
      _system_identifier.Append((char)codepoint);
    }
    public void init_public_identifier() {
      _public_identifier = new StringBuilder();
    }
    public void init_system_identifier() {
      _system_identifier = new StringBuilder();
    }
  }

  public class Tag {
    StringBuilder _name = new StringBuilder();
    public string name { get { return _name.ToString(); } }
    public bool self_closing;
    public List<HTMLAttribute>? attributes;

    public override string ToString() {
      StringBuilder sb = new StringBuilder();
      if (attributes != null) {
        foreach (var attr in attributes) {
          sb.Append($"\"{attr.name}\"=\"{attr.value}\" ");
        }
      }
      return $"{{ name: '{_name}', self_closing: '{self_closing}', attributes: {sb} }}";
    }

    public void append_to_name(int codepoint) {
      _name.Append((char)codepoint);
    }

    public void start_new_attribute() {
      start_new_attribute("", "");
    }
    public void start_new_attribute(int codepoint, string value) {
      start_new_attribute($"{(char)codepoint}", value);
    }

    public void start_new_attribute(string name, string value) {
      if (attributes == null) attributes = new List<HTMLAttribute>();
      attributes.Add(new HTMLAttribute(name, value));
    }

    public void append_to_current_attribute_name(int codepoint) {
      // Console.WriteLine($"append_to_current_attribute_name: {codepoint}");
      Debug.Assert(attributes?.Count > 0);
      attributes.Last().name.Append((char)codepoint);
    }

    public void append_to_current_attribute_value(int codepoint) {
      // Console.WriteLine($"append_to_current_attribute_value: {codepoint}");
      Debug.Assert(attributes?.Count > 0);
      attributes.Last().value.Append((char)codepoint);
    }
  }

  public class Data {
    readonly StringBuilder _data = new StringBuilder();
    public string data { get { return _data.ToString(); } }
    public override string ToString() {
      return _data.ToString();
    }
    public void append_to_data(int codepoint) {
      _data.Append((char)codepoint);
    }
    public void append_to_data(string str) {
      _data.Append(str);
    }

    public int first_character { get { return _data.ToString()[0]; } }
  }

  public Type type { get { return _type; } }
  public Tag tag { get { return _tag!; } }
  public DocType doctype { get { return _doctype!; } }
  public Data comment_or_character { get { return _comment_or_character!; } }

  public override string ToString() {
    return _type switch {
      Type.DOCTYPE => $"DOCTYPE {_doctype}",
      Type.StartTag => $"StartTag {_tag}",
      Type.EndTag => $"EndTag {_tag}",
      Type.Comment => $"Comment '{_comment_or_character}'",
      Type.Character => $"Character '{_comment_or_character}'",
      Type.EndOfFile => $"EndOfFile",
      _ => "",
    };
  }

  public static HTMLToken create_character_token(int codepoint) {
    var token = new HTMLToken(Type.Character);
    token.comment_or_character.append_to_data(codepoint);
    return token;
  }

  public bool is_eof() {
    return _type == Type.EndOfFile;
  }

  public string? get_attribute_value(string name) {
    return _tag!.attributes?.Find(attr => attr.name.ToString() == name)?.value?.ToString();
  }
}

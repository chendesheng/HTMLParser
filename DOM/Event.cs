global using EventHandler = System.Action<Event>;

class Event {
  public string type { get; set; }
  public EventTarget? target { get; private set; }
  public EventTarget? src_element { get; private set; }
  public EventTarget? current_target { get; private set; }

  public void stop_propagation() { }
  public bool bubbles { get; private set; }
  public bool cancelable { get; private set; }

  public void prevent_default() { }
  public bool default_prevented { get; private set; }

  public void init_event(string type, bool bubbles = false, bool cancelable = false) {
  }
}


class AbortSignal {

}

struct AddEventListenerOptions {
  public bool passive;
  public bool once;
  public AbortSignal signal;
}

interface EventTarget {
  bool dispatch_event(Event e);
  void add_event_listener(string type, EventHandler? callback = null);
  void remove_event_listener(string type, EventHandler? callback = null);
}

interface GlobalEventHandler {
  EventHandler onclick { get; set; }
}

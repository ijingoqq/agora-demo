using System;
using System.Collections.Generic;
using UnityEngine;

public class Loom : MonoBehaviour {
    private static List<Action> _actions = new List<Action>();
    private static List<Action> _currentActions = new List<Action>();
    private static Loom _current;

    public static Loom Current {
        get {
            Initialize();
            return _current;
        }
    }

    void Awake() {
        _current = this;
    }

    public static void Initialize() {
        if (_current == null) {
            GameObject g = new GameObject("Loom");
            _current = g.AddComponent<Loom>();
        }
    }

    public static void QueueOnMainThread(Action action) {
        lock (_actions) {
            _actions.Add(action);
        }
    }

    void Update() {
        lock (_actions) {
            _currentActions.Clear();
            _currentActions.AddRange(_actions);
            _actions.Clear();
        }

        foreach (var action in _currentActions) {
            action?.Invoke();
        }
    }
}

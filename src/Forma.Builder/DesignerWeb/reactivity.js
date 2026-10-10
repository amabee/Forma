// Runtime-only reactive scopes. Every effect and binding belongs to its component behavior.
(() => {
  let active;
  const pending = new Set(), scopes = new Set();
  let flushing = false;
  function flush() {
    if (flushing) return;
    flushing = true;
    const counts = new Map();
    try {
      while (pending.size) {
        const effect = pending.values().next().value;
        pending.delete(effect);
        const count = (counts.get(effect) ?? 0) + 1;
        counts.set(effect, count);
        if (count > 100) {
          effect.dispose();
          effect.error(new Error("Reactive effect did not stabilize. Avoid writing back to its own dependencies."));
        } else effect.run();
      }
    } finally { flushing = false; }
  }
  function signal(initial, equal = Object.is) {
    let value = initial;
    const dependents = new Set();
    return {
      get value() {
        if (active) { dependents.add(active); active.dependencies.add(dependents); }
        return value;
      },
      set value(next) {
        if (equal(value, next)) return;
        value = next;
        for (const effect of dependents) pending.add(effect);
        flush();
      }
    };
  }
  function untracked(callback) {
    const previous = active; active = undefined;
    try { return callback(); } finally { active = previous; }
  }
  function sameValue(a, b) {
    return Object.is(a, b) || a && b && typeof a === "object" && typeof b === "object" && JSON.stringify(a) === JSON.stringify(b);
  }
  function createScope(context) {
    let disposed = false;
    const disposers = new Set(), bindings = new Map();
    function safely(callback) {
      try { Promise.resolve(untracked(callback)).catch(context.error); }
      catch (error) { context.error(error); }
    }
    function effect(callback) {
      if (typeof callback !== "function") throw new Error("effect requires a function.");
      if (disposed) throw new Error("This reactive scope has been destroyed.");
      let cleanup, stopped = false;
      const task = {
        dependencies: new Set(), error: context.error,
        run() {
          if (stopped) return;
          for (const dependency of task.dependencies) dependency.delete(task);
          task.dependencies.clear();
          if (cleanup) { safely(cleanup); cleanup = undefined; }
          const previous = active; active = task;
          try {
            const result = callback();
            if (typeof result === "function") cleanup = result;
            else if (result?.then) Promise.resolve(result).catch(context.error);
          } catch (error) { context.error(error); }
          finally { active = previous; }
        },
        dispose() {
          if (stopped) return;
          stopped = true; pending.delete(task);
          for (const dependency of task.dependencies) dependency.delete(task);
          task.dependencies.clear(); disposers.delete(task.dispose);
          if (cleanup) { safely(cleanup); cleanup = undefined; }
        }
      };
      disposers.add(task.dispose);
      pending.add(task); flush();
      return task.dispose;
    }
    function watch(source, callback, options = {}) {
      if (typeof callback !== "function") throw new Error("watch requires a callback.");
      const read = typeof source === "function" ? source : () => source.value;
      let first = true, previous;
      return effect(() => {
        const next = read();
        if (first) {
          first = false; previous = next;
          if (options.immediate) safely(() => callback(next, undefined));
        } else if (!sameValue(next, previous)) {
          const old = previous; previous = next;
          safely(() => callback(next, old));
        }
      });
    }
    function ref(initial) {
      const cell = signal(initial);
      cell.subscribe = (callback, options) => watch(cell, callback, options);
      return cell;
    }
    function computed(callback) {
      if (typeof callback !== "function") throw new Error("computed requires a function.");
      const cell = ref(undefined);
      effect(() => { cell.value = callback(); });
      return { get value() { return cell.value; }, subscribe: cell.subscribe };
    }
    function reactive(initial) {
      if (!initial || Object.getPrototypeOf(initial) !== Object.prototype) throw new Error("reactive requires a plain object.");
      const properties = new Map(Object.entries(initial).map(([key, value]) => [key, signal(value)]));
      const shape = signal(0);
      return new Proxy({ ...initial }, {
        get(object, key) {
          if (typeof key === "symbol") return Reflect.get(object, key);
          if (!properties.has(key)) properties.set(key, signal(undefined));
          return properties.get(key).value;
        },
        set(object, key, value) {
          const added = !Object.hasOwn(object, key);
          Reflect.set(object, key, value);
          if (!properties.has(key)) properties.set(key, signal(undefined));
          properties.get(key).value = value;
          if (added) shape.value = untracked(() => shape.value) + 1;
          return true;
        },
        deleteProperty(object, key) {
          const existed = Object.hasOwn(object, key);
          Reflect.deleteProperty(object, key);
          if (existed) {
            if (properties.has(key)) properties.get(key).value = undefined;
            shape.value = untracked(() => shape.value) + 1;
          }
          return true;
        },
        ownKeys(object) { shape.value; return Reflect.ownKeys(object); }
      });
    }
    function bind(name, property) {
      if (disposed) throw new Error("This reactive scope has been destroyed.");
      const id = context.resolve(name);
      const key = JSON.stringify([id, property]);
      if (bindings.has(key)) return bindings.get(key).reference;
      const cell = signal(context.read(id, property), sameValue);
      const reference = {
        get value() {
          cell.value; // Track invalidation while reading the latest DOM/model value.
          return context.read(id, property);
        },
        set value(value) {
          if (disposed) throw new Error("This reactive scope has been destroyed.");
          context.write(id, property, value);
        },
        subscribe(callback, options) { return watch(reference, callback, options); }
      };
      bindings.set(key, { id, property, cell, reference });
      return reference;
    }
    const scope = {
      ref, reactive, computed, effect, watch, bind, error: context.error,
      refresh(event) {
        if (disposed) return;
        for (const binding of bindings.values()) {
          const element = document.getElementById(binding.id);
          if (event && !element?.contains(event.target)) continue;
          const value = element ? context.read(binding.id, binding.property) : undefined;
          binding.cell.value = value;
        }
      },
      dispose() {
        if (disposed) return;
        disposed = true; scopes.delete(scope);
        for (const dispose of [...disposers]) dispose();
        bindings.clear();
      }
    };
    scopes.add(scope);
    return scope;
  }
  function refresh(event) {
    for (const scope of scopes) {
      try { scope.refresh(event); } catch (error) { scope.error(error); }
    }
  }
  for (const event of ["input", "change"]) document.addEventListener(event, refresh, true);
  for (const event of ["click", "row-selection"]) document.addEventListener(event, refresh);
  window.formaReactivity = { createScope, refresh };
})();

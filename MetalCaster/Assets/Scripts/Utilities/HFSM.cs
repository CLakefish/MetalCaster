using System;
using System.Collections.Generic;
using UnityEngine;

namespace HFSMFramework
{
    public interface IState
    {
        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void Exit() { }
    }

    public class State<T> : IState
    {
        protected T context;
        public State(T context) => this.context = context;

        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void Exit() { }
    }

    public class StateMachine<T> : IState
    {
        public IState CurrentState           { get; private set; }
        public IState PreviousState          { get; private set; }
        public float Duration                { get; private set; }

        private readonly Dictionary<IState, List<Transition>> Transitions = new();
        private readonly List<System.Action> onChange = new();

        protected T context;

        public StateMachine(T context) => this.context = context;

        public void Start(IState startState)
        {
            Duration     = 0;
            CurrentState = startState;
            CurrentState?.Enter();
        }

        public void AddTransitions(List<Transition> transitions)
        {
            List<Transition> GlobalTransitions = new();

            for (int i = 0; i < transitions.Count; i++)
            {
                if (transitions[i].TransitionState == transitions[i].CurrentState) continue;

                if (transitions[i].CurrentState == null) {
                    GlobalTransitions.Add(transitions[i]);

                    foreach (var pair in Transitions) {
                        pair.Value.Add(transitions[i]);
                    }

                    continue;
                }

                if (!Transitions.ContainsKey(transitions[i].CurrentState)) {
                    Transitions.Add(transitions[i].CurrentState, new List<Transition>() { transitions[i] });

                    foreach (var global in GlobalTransitions) {
                        Transitions[transitions[i].CurrentState].Add(global);
                    }
                }
                else {
                    Transitions[transitions[i].CurrentState].Add(transitions[i]);
                }
            }
        }

        public void AddOnChange(List<System.Action> actions) => onChange.AddRange(actions);

        public void Update()
        {
            CurrentState?.Update();
            Duration += Time.deltaTime;
        }

        public void FixedUpdate()
        {
            CurrentState?.FixedUpdate();
        }

        public void ChangeState(IState newState)
        {
            PreviousState = CurrentState;
            CurrentState?.Exit();

            foreach (var action in onChange) action?.Invoke();

            Duration = 0;

            CurrentState = newState;
            CurrentState?.Enter();
        }

        public void CheckTransitions()
        {
            foreach (Transition transition in Transitions[CurrentState]) {
                if (transition.Condition()) {
                    ChangeState(transition.TransitionState);
                    break;
                }
            }
        }

        public void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 200));

            string current   = "Current State : "  + $"<color='white'>{(CurrentState == null  ? "None" : CurrentState)} </color>",
                   previous  = "Previous State : " + $"<color='white'>{(PreviousState == null ? "None" : PreviousState)}</color>";

            GUILayout.Label($"<size=18>{current } </size>");
            GUILayout.Label($"<size=18>{previous}</size>");

            GUILayout.Label($"<size=15>{Duration}</size>");

            GUILayout.EndArea();
        }
    }

    public class Transition
    {
        public IState TransitionState;
        public IState CurrentState;
        public Func<bool> Condition;

        public Transition(IState DesiredStateTransition, IState TransitionedState, Func<bool> Condition)
        {
            this.Condition = Condition;
            this.TransitionState = TransitionedState;
            this.CurrentState = DesiredStateTransition;
        }
    }
}

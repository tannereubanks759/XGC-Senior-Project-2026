/*
 * This script defines the abstract base class for managing AI state machines.
 * All concrete AI controllers will inherit from this class and use it to 
 * handle transitions between different states (Idle, Patrol, Attack, etc.).
 * 
 * By: Matthew Bolger
 */

using System.Collections.Generic;
using System;
using UnityEngine;

// Abstract base class for any AI State Manager.
// Uses a generic type parameter <EState> that must be an Enum.
// This lets the state machine work with any enum-defined state type 
// (States are defined in EnemyState.cs).
public abstract class StateManager<EState> : MonoBehaviour where EState : Enum
{
    // Dictionary that maps a state key (the enum value) to its corresponding state instance.
    // Example: States[EnemyState.Patrol] gives the Patrol state object.
    protected Dictionary<EState, BaseState<EState>> States = new Dictionary<EState, BaseState<EState>>();

    // Tracks the currently active state.
    protected BaseState<EState> CurrentState;

    // Flag to prevent multiple state transitions from happening at the same time.
    protected bool IsTransitioningState = false;

    // Unity lifecycle method, called once when the script starts.
    // Ensures the initial state's EnterState() is called at startup.
    void Start()
    {
        CurrentState.EnterState();
    }

    // Unity lifecycle method, called every frame.
    // Handles updating the current state and checking for transitions.
    protected virtual void Update()
    {
        // Ask the current state what the next state should be.
        EState nextStateKey = CurrentState.GetNextState();

        // If the next state is the same as the current one, just keep updating it.
        if (nextStateKey.Equals(CurrentState.StateKey))
        {
            CurrentState.UpdateState();
        }
        // Otherwise, perform a transition to the new state.
        else if (!IsTransitioningState)
        {
            TransitionToState(nextStateKey);
        }
    }

    // Handles the logic for leaving the current state and switching to a new one.
    public void TransitionToState(EState stateKey)
    {
        IsTransitioningState = true;

        // Exit the current state.
        CurrentState.ExitState();

        // Switch to the new state by looking it up in the dictionary.
        CurrentState = States[stateKey];

        // Enter the new state.
        CurrentState.EnterState();

        IsTransitioningState = false;
    }
}

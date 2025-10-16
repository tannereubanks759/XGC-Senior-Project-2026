/*
 * This script defines the abstract base class that all individual 
 * AI states (Idle, Patrol, Attack, etc.) will inherit from.
 * It enforces a consistent structure for states by requiring them 
 * to implement Enter, Exit, Update, and transition logic.
 * 
 * By: Matthew Bolger
 */

using UnityEngine;
using System;

// Abstract base class for any state used in the StateManager.
// Uses a generic type parameter <EState> which must be an Enum.
// This makes sure the state is tied to a defined set of state keys 
// (like EnemyState.Idle, EnemyState.Attack, etc.).
public abstract class BaseState<EState> where EState : Enum
{
    // The enum value that uniquely identifies this state.
    // Example: EnemyState.Patrol, EnemyState.Attack, etc.
    public EState StateKey { get; private set; }

    // Constructor that assigns the state key when the state is created.
    public BaseState(EState key)
    {
        StateKey = key;
    }

    // Called when the state is first entered.
    // Used for setup such as playing an animation or resetting variables.
    public abstract void EnterState();

    // Called when the state is about to exit.
    // Used for cleanup or stopping certain actions before transitioning away.
    public abstract void ExitState();

    // Called once per frame while the state is active.
    // Contains the main behavior logic for this state.
    public abstract void UpdateState();

    // Determines which state should come next.
    // Returns the enum value for the next state.
    // If it returns its own key, the state machine stays in this state.
    public abstract EState GetNextState();
}
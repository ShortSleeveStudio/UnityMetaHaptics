using UnityEngine;
using UnityEngine.InputSystem;

namespace Studio.ShortSleeve.UnityMetaHaptics.Common
{
    /// <summary>
    /// Represents an active haptic playback session. Can be awaited or used to stop playback.
    /// </summary>
    public struct HapticResponse
    {
        #region State
        long _id;
        Gamepad _gamepad;
        Awaitable _awaitable;
        IHapticsPlayer _parent;
        #endregion

        #region Public Properties
        /// <summary>
        /// Unique identifier for this haptic playback session.
        /// </summary>
        public long ID => _id;

        /// <summary>
        /// The gamepad device this haptic is playing on.
        /// </summary>
        public Gamepad GamepadDevice => _gamepad;
        #endregion

        #region Internal Properties
        internal Awaitable InternalAwaitable => _awaitable;
        #endregion

        #region Constructor
        internal HapticResponse(
            long id,
            Gamepad gamepad,
            Awaitable awaitable,
            IHapticsPlayer parent
        )
        {
            _id = id;
            _gamepad = gamepad;
            _awaitable = awaitable;
            _parent = parent;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Stops this haptic playback immediately.
        /// </summary>
        public void Stop()
        {
            if (_parent == null)
                return;
            _parent.Stop(this);
        }

        /// <summary>
        /// Allows awaiting the completion of haptic playback using async/await.
        /// </summary>
        /// <returns>An awaiter for the underlying Awaitable.</returns>
        /// <exception cref="System.OperationCanceledException">Thrown if the haptic response is no longer valid or if awaitable is null.</exception>
        public Awaitable.Awaiter GetAwaiter()
        {
            if (_parent == null || _awaitable == null || !_parent.IsValid(this))
                throw new System.OperationCanceledException();
            return _awaitable.GetAwaiter();
        }
        #endregion
    }
}

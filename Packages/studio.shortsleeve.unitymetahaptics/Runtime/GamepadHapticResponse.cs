using UnityEngine;
using UnityEngine.InputSystem;

namespace Studio.ShortSleeve.UnityMetaHaptics
{
    public struct GamepadHapticResponse
    {
        #region State
        long _id;
        Gamepad _gamepad;
        Awaitable _awaitable;
        GamepadHaptics _parent;
        #endregion

        #region Public Properties
        public long ID => _id;
        public Gamepad Device => _gamepad;
        #endregion

        #region Internal Properties
        internal Awaitable InternalAwaitable => _awaitable;
        #endregion

        #region Constructor
        internal GamepadHapticResponse(
            long id,
            Gamepad gamepad,
            Awaitable awaitable,
            GamepadHaptics parent
        )
        {
            _id = id;
            _gamepad = gamepad;
            _awaitable = awaitable;
            _parent = parent;
        }
        #endregion

        #region Public API
        public void Stop() => _parent.Stop(this);

        public Awaitable.Awaiter GetAwaiter()
        {
            if (!_parent.IsValid(this))
                throw new System.OperationCanceledException();
            return _awaitable.GetAwaiter();
        }
        #endregion
    }
}

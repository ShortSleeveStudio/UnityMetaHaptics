using UnityEngine;

namespace Studio.ShortSleeve.UnityMetaHaptics.Common
{
    public struct GamepadHapticResponse<T>
    {
        #region State
        long _id;
        T _device;
        Awaitable _awaitable;
        IHapticsPlayer<T> _parent;
        #endregion

        #region Public Properties
        public long ID => _id;
        public T Device => _device;
        #endregion

        #region Internal Properties
        internal Awaitable InternalAwaitable => _awaitable;
        #endregion

        #region Constructor
        internal GamepadHapticResponse(
            long id,
            T device,
            Awaitable awaitable,
            IHapticsPlayer<T> parent
        )
        {
            _id = id;
            _device = device;
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

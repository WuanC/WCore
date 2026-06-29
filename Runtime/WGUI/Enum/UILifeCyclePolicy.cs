using UnityEngine;

namespace WGUI
{
    public enum UILifeCyclePolicy
    {

        /// <summary>
        /// Asset được load khi Show() lần đầu
        /// Instance chỉ được tạo khi cần và disable khi không còn sử dụng.
        /// </summary>
        SaveInstance,

        /// <summary>
        /// Asset được load khi Show() và release khi Close().
        /// Ínstance được tạo khi Show() và destroy khi Close().
        /// </summary>
        ReleaseInstance,

    }
}
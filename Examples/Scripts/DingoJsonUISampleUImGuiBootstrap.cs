using UnityEngine;

namespace DingoJsonUI.Examples.Scripts
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(UImGui.UImGui))]
    public sealed class DingoJsonUISampleUImGuiBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            var cameraComponent = GetComponent<Camera>();
            var uImGui = GetComponent<UImGui.UImGui>();
            if (uImGui.Camera != cameraComponent)
                uImGui.SetCamera(cameraComponent);
        }
    }
}

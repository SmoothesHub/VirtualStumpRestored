using UnityEngine;
using UnityEngine.SceneManagement;

namespace VirtualStumpRestored;

internal sealed class TeleporterBinding
{
    private const string RelativePath = "LocalObjects_Prefab/TreeRoom/VirtualStump_HeadsetTeleporter";
    public Transform Root { get; }
    public Transform Monitor { get; }
    public MeshFilter Body { get; }
    public MeshFilter LeftHandle { get; }
    public MeshFilter RightHandle { get; }
    public BoxCollider HeadArea { get; }
    public VirtualStumpTeleporter NativeTeleporter { get; }
    public TMPro.TMP_Text FontSource { get; }
    public bool Valid => Root != null && Monitor != null && Body != null && HeadArea != null && NativeTeleporter != null &&
                         NativeTeleporter.GetReturnTransform() != null && LeftHandle != null && RightHandle != null;

    private TeleporterBinding(Transform root, Transform monitor, MeshFilter body, MeshFilter left, MeshFilter right,
        BoxCollider headArea, VirtualStumpTeleporter teleporter, TMPro.TMP_Text fontSource)
    {
        Root = root; Monitor = monitor; Body = body; LeftHandle = left; RightHandle = right;
        HeadArea = headArea; NativeTeleporter = teleporter; FontSource = fontSource;
    }

    public static TeleporterBinding? Find()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (var sceneRoot in scene.GetRootGameObjects())
            {
                if (sceneRoot.name != "Environment Objects") continue;
                var root = sceneRoot.transform.Find(RelativePath);
                if (root == null) continue;
                var monitor = root.parent.Find("TreeRoomInteractables/GorillaComputerObject/ComputerUI/monitor");
                if (monitor == null) continue;
                var body = root.Find("stumpheadset")?.GetComponent<MeshFilter>();
                var left = root.Find("Handle Left")?.GetComponent<MeshFilter>();
                var right = root.Find("Handle Right")?.GetComponent<MeshFilter>();
                var area = root.Find("TeleporterTrigger")?.GetComponent<BoxCollider>();
                var native = area != null ? area.GetComponent<VirtualStumpTeleporter>() : null;
                var font = root.GetComponentInChildren<TMPro.TMP_Text>(true);
                if (body == null || left == null || right == null || area == null || native == null || font == null ||
                    body.sharedMesh == null || left.sharedMesh == null || right.sharedMesh == null ||
                    native.GetReturnTransform() == null || font.font == null) return null;
                return new TeleporterBinding(root, monitor, body, left, right, area, native, font);
            }
        }
        return null;
    }
}

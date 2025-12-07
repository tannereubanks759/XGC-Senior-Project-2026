using UnityEngine;

[AddComponentMenu("Camera/Match All Cameras To Main FOV")]
[ExecuteAlways] // Works in Play Mode and Edit Mode
public class MatchAllCamerasToMainFOV : MonoBehaviour
{
    [Tooltip("If true, FOV will be synced every frame. If false, it only syncs on enable or when you call SyncNow().")]
    public bool runEveryFrame = true;

    [Tooltip("If true, will also sync disabled cameras and scene cameras in the editor.")]
    public bool includeDisabledCameras = false;

    private Camera mainCam;

    void OnEnable()
    {
        CacheMainCamera();
        SyncNow();
    }

    void Update()
    {
        if (!runEveryFrame)
            return;

        if (mainCam == null)
            CacheMainCamera();

        SyncNow();
    }

    private void CacheMainCamera()
    {
        mainCam = Camera.main;
    }

    /// <summary>
    /// Manually sync all cameras to the main camera FOV.
    /// </summary>
    [ContextMenu("Sync Now")]
    public void SyncNow()
    {
        if (mainCam == null)
            CacheMainCamera();

        if (mainCam == null)
            return;

        Camera[] cameras;

        if (includeDisabledCameras)
        {
#if UNITY_EDITOR
            // In editor, this finds cameras even if disabled / not in play mode
            cameras = Resources.FindObjectsOfTypeAll<Camera>();
#else
            cameras = Camera.allCameras;
#endif
        }
        else
        {
            cameras = Camera.allCameras;
        }

        float fov = mainCam.fieldOfView;

        foreach (var cam in cameras)
        {
            if (cam == null || cam == mainCam)
                continue;

            cam.fieldOfView = fov;
        }
    }
}

using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
	public static ThirdPersonCamera Instance { get; private set; }

	public enum FollowMode { LockedBehind, Free }

	[Header("General")]
	[Tooltip("Follow behavior. LockedBehind: offset is applied in the target's local space (player-back). Free: camera follows the player's world position + offset (Hades-like).")]
	public FollowMode followMode = FollowMode.Free;

	[Tooltip("Transform to follow (player)")]
	public Transform target;

	[Tooltip("Offset from the target. If FollowMode.LockedBehind, offset is in target local space; if Free, offset is world-space relative to player position.")]
	public Vector3 offset = new Vector3(0f, 2f, -6f);

	[Tooltip("Smooth time for camera position movement")]
	public float followSmoothTime = 0.12f;

	[Tooltip("Rotation speed (higher = faster)")]
	public float rotationSpeed = 10f;

	[Header("Free Follow Settings")]
	[Tooltip("When using Free follow mode, this multiplier applies a look-ahead based on player movement speed to keep the camera in front of motion.")]
	public float lookAheadMultiplier = 0.15f;

	[Tooltip("Maximum look-ahead distance (world units)")]
	public float maxLookAhead = 2f;

	[Tooltip("Radius around the player where the camera will not move (helps reduce micro jitter)")]
	public float deadZoneRadius = 0.2f;

	[Header("Collision")]
	[Tooltip("Radius for spherecast used when testing camera collision with world geometry.")]
	public float collisionRadius = 0.25f;

	[Tooltip("Small offset to keep the camera slightly off the collision surface")]
	public float collisionOffset = 0.12f;

	[Tooltip("Minimum distance from the pivot the camera is allowed to be (prevents camera snapping into player's head)")]
	public float minDistance = 0.5f;

	[Tooltip("Layers considered for camera collision (set to environment layers)")]
	public LayerMask collisionLayers = ~0;

	[Header("Follow Camera Mode (optional)")]
	[Tooltip("When enabled, the script will follow the specified Camera's transform instead of a target Transform.")]
	public bool followCamera = false;

	[Tooltip("Camera to follow when 'Follow Camera Mode' is active. If left empty, uses Camera.main.")]
	public Camera cameraToFollow;

	private Vector3 currentVelocity = Vector3.zero;
	private Vector3 lastTargetPosition = Vector3.zero;

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(this);
			return;
		}

		Instance = this;

		// Auto-assign target to object tagged 'Player' if none assigned
		if (target == null)
		{
			var go = GameObject.FindWithTag("Player");
			if (go != null)
				target = go.transform;
		}

		lastTargetPosition = (target != null) ? target.position : Vector3.zero;
	}

	void LateUpdate()
	{
		// If follow-camera mode active, just mirror that camera (useful for editor/demo)
		if (followCamera)
		{
			if (cameraToFollow == null)
				cameraToFollow = Camera.main;

			if (cameraToFollow == null)
				return;

			Vector3 desiredCamPosition = cameraToFollow.transform.TransformPoint(offset);
			transform.position = Vector3.SmoothDamp(transform.position, desiredCamPosition, ref currentVelocity, followSmoothTime);
			transform.rotation = Quaternion.Slerp(transform.rotation, cameraToFollow.transform.rotation, rotationSpeed * Time.deltaTime);
			return;
		}

		if (target == null)
			return;

		Vector3 desiredPosition;
		if (followMode == FollowMode.LockedBehind)
		{
			// Offset is treated in target's local space
			desiredPosition = target.TransformPoint(offset);
		}
		else // Free follow (Hades-like): offset is world-space relative to player's position
		{
			desiredPosition = target.position + offset;

			// Compute simple look-ahead from target movement (only horizontal plane XZ)
			Vector3 targetVelocity = Vector3.zero;
			if (Time.deltaTime > 0f)
				targetVelocity = (target.position - lastTargetPosition) / Time.deltaTime;

			Vector3 horizontalVel = new Vector3(targetVelocity.x, 0f, targetVelocity.z);
			Vector3 lookAhead = Vector3.ClampMagnitude(horizontalVel * lookAheadMultiplier, maxLookAhead);

			// Apply dead zone: only move camera when player leaves the small radius
			Vector3 toPlayer = (target.position + offset) - transform.position;
			if (toPlayer.sqrMagnitude > deadZoneRadius * deadZoneRadius)
			{
				desiredPosition += lookAhead;
			}
		}

		// Collision handling: spherecast from pivot (player eye height) towards desired camera position
		Vector3 pivot = target.position + Vector3.up * 1.2f;
		Vector3 dirToDesired = desiredPosition - pivot;
		float desiredDist = dirToDesired.magnitude;
		Vector3 finalPosition = desiredPosition;

		if (desiredDist > 0f)
		{
			RaycastHit hit;
			// Use SphereCast to account for camera radius
			if (Physics.SphereCast(pivot, collisionRadius, dirToDesired.normalized, out hit, desiredDist, collisionLayers, QueryTriggerInteraction.Ignore))
			{
				// Place camera slightly before the hit point to avoid clipping
				finalPosition = hit.point - dirToDesired.normalized * collisionOffset;
			}

			// Enforce minimum distance from pivot
			float finalDist = (finalPosition - pivot).magnitude;
			if (finalDist < minDistance)
			{
				finalPosition = pivot + dirToDesired.normalized * minDistance;
			}
		}

		// Smoothly move the camera (toward the collision-adjusted position)
		transform.position = Vector3.SmoothDamp(transform.position, finalPosition, ref currentVelocity, followSmoothTime);

		// Smoothly rotate to look at the target (slightly above the target's position)
		Vector3 lookAtPosition = target.position + Vector3.up * 1.2f;
		Quaternion desiredRotation = Quaternion.LookRotation(lookAtPosition - transform.position);
		transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);

		lastTargetPosition = target.position;
	}

	/// <summary>
	/// Static helper: enable/disable follow-camera mode and optionally set the camera to follow.
	/// </summary>
	public static void SetFollowCameraMode(bool enable, Camera cam = null)
	{
		if (Instance == null)
			return;

		Instance.followCamera = enable;
		if (cam != null)
			Instance.cameraToFollow = cam;
		else if (enable && Instance.cameraToFollow == null)
			Instance.cameraToFollow = Camera.main;
	}

	/// <summary>
	/// Set the camera target at runtime.
	/// </summary>
	public void SetTarget(Transform t)
	{
		target = t;
		lastTargetPosition = (t != null) ? t.position : Vector3.zero;
	}
}


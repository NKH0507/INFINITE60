using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gridSize = 1f;

    [Header("3D Physics References")]
    [SerializeField] private LayerMask objectLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask floorLayer;

    private bool isMoving;
    private VisualEffects vfx;
    public bool IsMoving => isMoving;

    private void Awake()
    {
        vfx = GetComponent<VisualEffects>();
    }//wewew

    private void Update()
    {
        if (isMoving) return;

        Vector3 moveDirection = Vector3.zero;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) moveDirection = Vector3.forward;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) moveDirection = Vector3.back;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) moveDirection = Vector3.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) moveDirection = Vector3.right;

        if (moveDirection != Vector3.zero)
        {
            TryMove(moveDirection);
        }
    }

    private void TryMove(Vector3 direction)
    {
        Vector3 targetPos = transform.position + direction * gridSize;

        if (!CanMove(targetPos))
        {
            if (vfx != null) vfx.PlayBlockedAnimation(direction);
            Debug.Log("Player cannot move to target position.");
            return;
        }

        bool hasObject = Physics.Raycast(
            transform.position,
            direction,
            out RaycastHit hit,
            gridSize * 0.9f,
            objectLayer);

        if (hasObject && hit.collider != null)
        {
            Box box = hit.collider.GetComponentInParent<Box>();
            Bush bush = hit.collider.GetComponentInParent<Bush>();

            if (box != null)
            {
                if (box.TryPush(direction, gridSize))
                {
                    TurnManager.Instance.SaveState();
                    StartCoroutine(MoveSmoothly(targetPos));
                }
                else
                {
                    if (vfx != null) vfx.PlayBlockedAnimation(direction);
                    Debug.Log("Box cannot be pushed.");
                }
            }
            else if (bush != null)
            {
                if (vfx != null) vfx.PlayBlockedAnimation(direction);
                Debug.Log("Bush cannot be pushed.");
            }
        }
        else
        {
            TurnManager.Instance.SaveState();
            StartCoroutine(MoveSmoothly(targetPos));
        }
    }

    private bool CanMove(Vector3 targetPos)
    {
        Collider[] walls = Physics.OverlapSphere(targetPos, 0.1f, wallLayer);
        if (walls.Length > 0)
        {
            Debug.Log("Blocked by wall.");
            return false;
        }

        if (Physics.Raycast(targetPos + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 1.6f, floorLayer))
        {
            Floor floor = hit.collider.GetComponent<Floor>();
            if (floor != null && (floor.Type == Floor.FloorType.Wall || floor.Type == Floor.FloorType.Water))
            {
                Debug.Log("Blocked by floor type.");
                return false;
            }
        }
        else
        {
            Debug.Log("No floor under target.");
            return false;
        }

        return true;
    }

    private IEnumerator MoveSmoothly(Vector3 target)
    {
        isMoving = true;
        Vector3 start = transform.position;
        Vector3 direction = (target - start).normalized;
        if (vfx != null) vfx.PlayMoveAnimation(direction);

        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = target;
        isMoving = false;
        CheckTileUnderneath();
    }

    private void CheckTileUnderneath()
    {
        if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 1.6f, floorLayer))
        {
            return;
        }

        Floor floor = hit.collider.GetComponent<Floor>();
        if (floor == null) return;

        if (floor.Type == Floor.FloorType.Fire)
        {
            Debug.Log("Player stepped on fire. Restarting scene.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else if (floor.Type == Floor.FloorType.Goal)
        {
            if (vfx != null) vfx.PlayGoalAnimation();
            Debug.Log("Goal reached.");
        }
    }
}

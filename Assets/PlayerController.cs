using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _speed = 5.0f;
    [SerializeField] private float _sideMove = 2.25f;
    [SerializeField] private float _swipeThreshold = 0.1f;

    private PlayerInput _playerInput;
    private Vector2 _touchPosition;
    private Rigidbody _rb;

    private InputAction _moveAction;
    private InputAction _jumpAction;

    private float _maxLeft = -2.25f;
    private float _maxRight = 2.25f;

    bool _canMove;
    bool _canJump;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("PlayerInput component not found on GameObject");
        }
        else
        {
            _moveAction = _playerInput.actions["Move"];
            Debug.Log("Move action found: " + _moveAction);
            _jumpAction = _playerInput.actions["Jump"];
            Debug.Log("Jump action found: " + _jumpAction);
        }
    }

    private void OnEnable()
    {
        _moveAction.performed += Move;
        _jumpAction.performed += Jump;
    }

    private void OnDisable()
    {
        _moveAction.performed -= Move;
        _jumpAction.performed -= Jump;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _canMove = true;
        _canJump = true;
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    void Move(InputAction.CallbackContext context)
    {
        Vector2 delta = context.ReadValue<Vector2>();

        if (delta.x < -_swipeThreshold || delta.x > _swipeThreshold)
        {
            if (_canMove)
            {
                if (delta.x > 0)
                {
                    transform.position = new Vector3(
                        Mathf.Clamp(transform.position.x + _sideMove, _maxLeft, _maxRight),
                        transform.position.y,
                        transform.position.z);
                }
                else
                {
                    transform.position = new Vector3(
                        Mathf.Clamp(transform.position.x - _sideMove, _maxLeft, _maxRight),
                        transform.position.y,
                        transform.position.z);
                }

                StartCoroutine(MovementRestriction());
            }
        }
    }

    void Jump(InputAction.CallbackContext context)
    {
        Debug.Log("Jump");

        if (_canJump)
        {
            _rb.AddForce(Vector3.up * 5, ForceMode.Impulse);
            StartCoroutine(JumpRestriction());
        }
    }

    IEnumerator MovementRestriction()
    {
        _canMove = false; 
        yield return new WaitForSeconds(0.5f);
        _canMove = true;
    }

    IEnumerator JumpRestriction()
    {
        _canJump = false;
        yield return new WaitUntil(() => _rb.linearVelocity.y == 0 && Physics.Raycast(transform.position, Vector3.down, 1.1f));
        _canJump = true;
    }
}

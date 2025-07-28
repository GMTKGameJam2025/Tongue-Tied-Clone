using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerChainContraint : MonoBehaviour
{
    [Header("Chain Settings")]
    public Transform anchorPoint;
    public float chainLength = 5.0f;
    public bool chainEnabled = true;

    [Header("Visual")]
    public bool showChain = true;
    public float chainWidth = 0.05f;
    public Material chainMaterial;

    private CharacterController _controller;
    private LineRenderer _line;

    void Start()
    {
        _controller = GetComponent<CharacterController>();

        if (showChain)
        {
            _line = gameObject.AddComponent<LineRenderer>();
            _line.positionCount = 2;
            _line.startWidth = chainWidth;
            _line.endWidth = chainWidth;
            _line.material = chainMaterial ?? new Material(Shader.Find("Sprites/Default"));
        }
    }

    void LateUpdate()
    {
        if (!chainEnabled || anchorPoint == null) return;

        Vector3 offset = transform.position - anchorPoint.position;

        if (offset.magnitude > chainLength)
        {
            Vector3 clampedPos = anchorPoint.position + offset.normalized * chainLength;
            Vector3 correction = clampedPos - transform.position;
            _controller.Move(correction);
        }

        if (showChain && _line != null)
        {
            _line.enabled = true;
            _line.SetPosition(0, anchorPoint.position);
            _line.SetPosition(1, transform.position);
        }
        else if (_line != null)
        {
            _line.enabled = false;
        }
    }
}

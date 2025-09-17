using UnityEngine;

public class TextAnimator : MonoBehaviour
{
    private static GameObject text;
    [Tooltip("Speed of the floating animation")]
    [SerializeField] private float speed = 2f;
    [Tooltip("How high the text floats up and down")]
    [SerializeField] private float amplitude = 0.5f;

    void Start()
    {
        text = this.gameObject;
    }

    void Update()
    {
        Vector3 startPos = text.transform.position;
        text.transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * speed) * amplitude;
    }
}

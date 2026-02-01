using UnityEngine;

public class MoveDown : MonoBehaviour
{
    [SerializeField] private float speed = 3f;

    private void Update()
    {
        transform.position += Vector3.down * speed * Time.deltaTime;
    }
}

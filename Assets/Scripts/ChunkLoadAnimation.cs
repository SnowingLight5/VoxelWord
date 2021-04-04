using UnityEngine;

public class ChunkLoadAnimation : MonoBehaviour {
    float speed = 1f;
    Vector3 targetPos;

    float waitTimer;
    float timer = 0f;

    private void Start() {

        waitTimer = Random.Range(0f, 3f);
        targetPos = transform.position;
        transform.position = new Vector3(transform.position.x, -VoxelData.chunkHeight, transform.position.z);
    }

    private void Update() {
        if (timer < waitTimer) {
            timer += Time.deltaTime;
            return;
        }

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * speed);
        if (targetPos.y - transform.position.y < 1f) {
            transform.position = targetPos;
            Destroy(this);
        }
    }

}

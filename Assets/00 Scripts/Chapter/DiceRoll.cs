using System;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DiceRoll : MonoBehaviour
{
    [Header("References")]
    public Transform[] diceFaces; // Kéo 6 Object con tương ứng mặt 1 đến 6 vào đây (theo thứ tự)
    public Rigidbody rb;

    private int diceIndex = -1;
    private bool hasStoppedRolling;
    private bool delayFinished;

    // Sự kiện static để báo cho UI biết kết quả (Trả về: Chỉ số xúc xắc, Kết quả mặt)
    public static event Action<int, int> OnDiceResult;

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        // Cài đặt chuẩn vật lý cho xúc xắc mượt mà như video
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void Update()
    {
        // Nếu chưa hết thời gian chờ ban đầu thì bỏ qua không kiểm tra vật lý
        if (!delayFinished) return;

        // Nếu xúc xắc chưa dừng hẳn và vận tốc bình phương (speed) tiến về 0
        if (!hasStoppedRolling && rb.linearVelocity.sqrMagnitude == 0f)
        {
            hasStoppedRolling = true;
            GetNumberOnTopFace();
        }
    }

    public void RollDice(float throwForce, float rollForce, int index)
    {
        diceIndex = index;
        hasStoppedRolling = false;
        delayFinished = false;

        // Tạo một chút lực biến động ngẫu nhiên để quỹ đạo bay không bao giờ trùng nhau
        float randomVariance = UnityEngine.Random.Range(-1f, 1f);

        // 1. Áp lực đẩy tiến về phía trước theo hướng bộ ném
        rb.AddForce(transform.forward * (throwForce + randomVariance), ForceMode.Impulse);

        // 2. Tạo vector xoắn ngẫu nhiên trên cả 3 trục X, Y, Z
        float randX = UnityEngine.Random.Range(0f, 1f);
        float randY = UnityEngine.Random.Range(0f, 1f);
        float randZ = UnityEngine.Random.Range(0f, 1f);
        Vector3 randomTorque = new Vector3(randX, randY, randZ).normalized;

        // Áp lực xoắn làm xúc xắc nhào lộn hỗn loạn
        rb.AddTorque(randomTorque * (rollForce + randomVariance), ForceMode.Impulse);

        // Kích hoạt bộ đếm thời gian chờ trước khi quét vật lý dừng
        DelayResult();
    }

    private async void DelayResult()
    {
        // Chờ 1 giây (1000 mili-giây) để xúc xắc tung tăng trên bàn trước khi check xem nó đứng yên chưa
        await Task.Delay(1000);
        delayFinished = true;
    }

    [ContextMenu("Get Top Face")] // Cho phép click chuột phải vào Component ở Inspector để test nhanh
    private int GetNumberOnTopFace()
    {
        if (diceFaces == null || diceFaces.Length == 0) return -1;

        int topFaceIndex = 0;
        // Lấy vị trí Y trong không gian thế giới của mặt đầu tiên làm mốc so sánh
        float lastYPosition = diceFaces[0].position.y;

        // Vòng lặp so sánh tọa độ Y của cả 6 mặt
        for (int i = 0; i < diceFaces.Length; i++)
        {
            if (diceFaces[i].position.y > lastYPosition)
            {
                lastYPosition = diceFaces[i].position.y;
                topFaceIndex = i; // Mặt nào cao nhất thì ghi nhận mặt đó
            }
        }

        int finalResult = topFaceIndex + 1; // Vì mảng bắt đầu từ 0 nên kết quả thực tế phải +1

        // Gửi kết quả đến hệ thống UI lắng nghe
        OnDiceResult?.Invoke(diceIndex, finalResult);

        Debug.Log($"Xúc xắc số {diceIndex} ra mặt: {finalResult}");
        UiHome.Instance.wall.SetActive(false);
        UiHome.Instance.rollPlane.SetActive(false);
        Destroy(gameObject); // Hủy gameObject xúc xắc sau 1 giây để tránh chồng lấp nhiều xúc xắc trên bàn
        return finalResult;
    }
}
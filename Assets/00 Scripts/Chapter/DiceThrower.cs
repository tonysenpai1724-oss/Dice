using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class DiceThrower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DiceRoll dicePrefab; // Kéo Prefab viên xúc xắc vào đây
    [SerializeField] private int amountOfDice = 2; // Số lượng xúc xắc muốn ném

    [Header("Physics Settings")]
    [SerializeField] private float throwForce = 5f; // Lực đẩy tiến tới
    [SerializeField] private float rollForce = 10f; // Lực xoắn nhào lộn
    private List<GameObject> spawnedDice = new List<GameObject>();
    void Start()
    {
        TigerForge.EventManager.StartListening(Constant.EVENT_ROLL_DICE, RollAllDice);
    }

    public async void RollAllDice()
    {
        if (dicePrefab == null) return;

        // Xóa sạch các viên xúc xắc cũ đã ném ở lượt trước để tránh chật bàn
        foreach (GameObject die in spawnedDice)
        {
            Destroy(die);
        }
        spawnedDice.Clear();
        UiHome.Instance.rollPlane.SetActive(true);
        UiHome.Instance.wall.SetActive(false);

        // Vòng lặp sinh ra số lượng xúc xắc cấu hình
        for (int i = 0; i < amountOfDice; i++)
        {
            // Tạo xúc xắc tại vị trí và góc xoay của bộ ném này
            DiceRoll newDie = Instantiate(dicePrefab, transform.position, transform.rotation);

            // Lưu vào danh sách quản lý
            spawnedDice.Add(newDie.gameObject);

            // Gọi hàm ném vật lý từ viên xúc xắc
            newDie.RollDice(throwForce, rollForce, i);
            await Task.Delay(1000);
            UiHome.Instance.wall.SetActive(true);

            // Chờ 1 Frame tiếp theo mới sinh viên tiếp theo để tránh hai viên chồng lấp va chạm lỗi nhau
            await Task.Yield();
        }
    }

}


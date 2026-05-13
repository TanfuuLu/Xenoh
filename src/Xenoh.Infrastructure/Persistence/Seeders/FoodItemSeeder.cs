using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Infrastructure.Persistence.Seeders;

public static class FoodItemSeeder
{
    public static List<(FoodItem Food, List<(string Label, decimal Grams)> Servings)> GetItems() =>
    [
        // ── Grains / Carbs ─────────────────────────────────────────────────────
        (new FoodItem { NameVi = "Cơm trắng (chín)",      NameEn = "White rice (cooked)",         CaloriesPer100g = 130, ProteinPer100g = 2.7m,  CarbsPer100g = 28.2m, FatPer100g = 0.3m, Source = FoodItemSource.Seed },
            [("bát", 200), ("chén nhỏ", 150)]),

        (new FoodItem { NameVi = "Cơm chiên",              NameEn = "Fried rice",                  CaloriesPer100g = 163, ProteinPer100g = 3.5m,  CarbsPer100g = 26.0m, FatPer100g = 5.0m, Source = FoodItemSource.Seed },
            [("phần", 250), ("bát", 200)]),

        (new FoodItem { NameVi = "Bún (chín)",             NameEn = "Rice vermicelli (cooked)",    CaloriesPer100g = 109, ProteinPer100g = 2.0m,  CarbsPer100g = 24.0m, FatPer100g = 0.3m, Source = FoodItemSource.Seed },
            [("tô nhỏ", 200), ("tô lớn", 300)]),

        (new FoodItem { NameVi = "Phở bò",                 NameEn = "Beef pho",                    CaloriesPer100g = 75,  ProteinPer100g = 5.0m,  CarbsPer100g = 9.0m,  FatPer100g = 1.5m, Source = FoodItemSource.Seed },
            [("tô", 500), ("tô lớn", 700)]),

        (new FoodItem { NameVi = "Bánh mì sandwich",       NameEn = "Sandwich bread",               CaloriesPer100g = 265, ProteinPer100g = 9.0m,  CarbsPer100g = 49.0m, FatPer100g = 3.2m, Source = FoodItemSource.Seed },
            [("lát", 30), ("ổ", 120)]),

        (new FoodItem { NameVi = "Yến mạch (khô)",         NameEn = "Oats (dry)",                  CaloriesPer100g = 389, ProteinPer100g = 17.0m, CarbsPer100g = 66.0m, FatPer100g = 7.0m, Source = FoodItemSource.Seed },
            [("chén", 80), ("muỗng lớn", 30)]),

        (new FoodItem { NameVi = "Khoai lang (hấp)",       NameEn = "Sweet potato (boiled)",       CaloriesPer100g = 86,  ProteinPer100g = 1.6m,  CarbsPer100g = 20.0m, FatPer100g = 0.1m, Source = FoodItemSource.Seed },
            [("củ vừa", 130), ("củ nhỏ", 80)]),

        (new FoodItem { NameVi = "Khoai tây (hấp)",        NameEn = "Potato (boiled)",             CaloriesPer100g = 87,  ProteinPer100g = 1.9m,  CarbsPer100g = 20.0m, FatPer100g = 0.1m, Source = FoodItemSource.Seed },
            [("củ vừa", 150), ("củ nhỏ", 80)]),

        (new FoodItem { NameVi = "Mì gói (nấu chín)",      NameEn = "Instant noodles (cooked)",    CaloriesPer100g = 138, ProteinPer100g = 3.4m,  CarbsPer100g = 20.0m, FatPer100g = 5.0m, Source = FoodItemSource.Seed },
            [("gói", 85)]),

        // ── Proteins ───────────────────────────────────────────────────────────
        (new FoodItem { NameVi = "Ức gà (luộc)",           NameEn = "Chicken breast (boiled)",     CaloriesPer100g = 165, ProteinPer100g = 31.0m, CarbsPer100g = 0.0m,  FatPer100g = 3.6m, Source = FoodItemSource.Seed },
            [("miếng vừa", 150), ("miếng lớn", 200)]),

        (new FoodItem { NameVi = "Ức gà (nướng)",          NameEn = "Chicken breast (grilled)",    CaloriesPer100g = 165, ProteinPer100g = 31.0m, CarbsPer100g = 0.0m,  FatPer100g = 3.6m, Source = FoodItemSource.Seed },
            [("miếng vừa", 150), ("miếng lớn", 200)]),

        (new FoodItem { NameVi = "Đùi gà (luộc)",          NameEn = "Chicken thigh (boiled)",      CaloriesPer100g = 177, ProteinPer100g = 24.0m, CarbsPer100g = 0.0m,  FatPer100g = 8.0m, Source = FoodItemSource.Seed },
            [("cái", 120)]),

        (new FoodItem { NameVi = "Thịt bò (nạc, luộc)",   NameEn = "Beef (lean, boiled)",         CaloriesPer100g = 215, ProteinPer100g = 26.0m, CarbsPer100g = 0.0m,  FatPer100g = 12.0m, Source = FoodItemSource.Seed },
            [("khẩu phần", 100)]),

        (new FoodItem { NameVi = "Thịt heo (nạc, luộc)",  NameEn = "Pork (lean, boiled)",         CaloriesPer100g = 185, ProteinPer100g = 26.0m, CarbsPer100g = 0.0m,  FatPer100g = 9.0m, Source = FoodItemSource.Seed },
            [("khẩu phần", 100)]),

        (new FoodItem { NameVi = "Trứng gà (luộc)",        NameEn = "Egg (boiled)",                CaloriesPer100g = 155, ProteinPer100g = 13.0m, CarbsPer100g = 1.1m,  FatPer100g = 11.0m, Source = FoodItemSource.Seed },
            [("quả vừa", 50), ("quả lớn", 60)]),

        (new FoodItem { NameVi = "Lòng trắng trứng (luộc)", NameEn = "Egg white (boiled)",        CaloriesPer100g = 52,  ProteinPer100g = 11.0m, CarbsPer100g = 0.7m,  FatPer100g = 0.2m, Source = FoodItemSource.Seed },
            [("quả", 35)]),

        (new FoodItem { NameVi = "Cá hồi (nướng)",         NameEn = "Salmon (grilled)",            CaloriesPer100g = 208, ProteinPer100g = 25.0m, CarbsPer100g = 0.0m,  FatPer100g = 12.0m, Source = FoodItemSource.Seed },
            [("miếng", 120)]),

        (new FoodItem { NameVi = "Cá ngừ (đóng hộp, nước)", NameEn = "Tuna (canned in water)",   CaloriesPer100g = 116, ProteinPer100g = 25.0m, CarbsPer100g = 0.0m,  FatPer100g = 1.0m, Source = FoodItemSource.Seed },
            [("hộp", 85)]),

        (new FoodItem { NameVi = "Đậu hũ (cứng)",          NameEn = "Tofu (firm)",                 CaloriesPer100g = 76,  ProteinPer100g = 8.0m,  CarbsPer100g = 2.0m,  FatPer100g = 4.0m, Source = FoodItemSource.Seed },
            [("miếng", 100), ("khẩu phần", 150)]),

        (new FoodItem { NameVi = "Sữa chua Hy Lạp (không đường)", NameEn = "Greek yogurt (plain)", CaloriesPer100g = 59,  ProteinPer100g = 10.0m, CarbsPer100g = 3.6m,  FatPer100g = 0.4m, Source = FoodItemSource.Seed },
            [("hũ", 150), ("chén", 200)]),

        (new FoodItem { NameVi = "Whey protein (1 muỗng)",  NameEn = "Whey protein (1 scoop)",     CaloriesPer100g = 370, ProteinPer100g = 73.0m, CarbsPer100g = 10.0m, FatPer100g = 3.0m, Source = FoodItemSource.Seed },
            [("muỗng", 30)]),

        // ── Fats / Healthy fats ────────────────────────────────────────────────
        (new FoodItem { NameVi = "Bơ (quả)",                NameEn = "Avocado",                     CaloriesPer100g = 160, ProteinPer100g = 2.0m,  CarbsPer100g = 9.0m,  FatPer100g = 15.0m, Source = FoodItemSource.Seed },
            [("quả vừa", 200), ("nửa quả", 100), ("phần nhỏ", 50)]),

        (new FoodItem { NameVi = "Dầu ô liu",               NameEn = "Olive oil",                   CaloriesPer100g = 884, ProteinPer100g = 0.0m,  CarbsPer100g = 0.0m,  FatPer100g = 100.0m, Source = FoodItemSource.Seed },
            [("muỗng canh", 14), ("muỗng cà phê", 5)]),

        (new FoodItem { NameVi = "Hạnh nhân",               NameEn = "Almonds",                     CaloriesPer100g = 579, ProteinPer100g = 21.0m, CarbsPer100g = 22.0m, FatPer100g = 50.0m, Source = FoodItemSource.Seed },
            [("nắm (28g)", 28), ("100g", 100)]),

        (new FoodItem { NameVi = "Đậu phộng rang",          NameEn = "Peanuts (roasted)",           CaloriesPer100g = 567, ProteinPer100g = 26.0m, CarbsPer100g = 16.0m, FatPer100g = 49.0m, Source = FoodItemSource.Seed },
            [("nắm", 30)]),

        (new FoodItem { NameVi = "Bơ đậu phộng",            NameEn = "Peanut butter",               CaloriesPer100g = 588, ProteinPer100g = 25.0m, CarbsPer100g = 20.0m, FatPer100g = 50.0m, Source = FoodItemSource.Seed },
            [("muỗng canh", 16), ("muỗng lớn", 32)]),

        // ── Vegetables ────────────────────────────────────────────────────────
        (new FoodItem { NameVi = "Rau xanh hỗn hợp",        NameEn = "Mixed greens",                CaloriesPer100g = 20,  ProteinPer100g = 2.0m,  CarbsPer100g = 3.0m,  FatPer100g = 0.3m, Source = FoodItemSource.Seed },
            [("chén", 100)]),

        (new FoodItem { NameVi = "Bông cải xanh (hấp)",     NameEn = "Broccoli (steamed)",          CaloriesPer100g = 35,  ProteinPer100g = 2.4m,  CarbsPer100g = 5.0m,  FatPer100g = 0.4m, Source = FoodItemSource.Seed },
            [("chén", 150), ("khẩu phần", 100)]),

        (new FoodItem { NameVi = "Cà chua",                  NameEn = "Tomato",                      CaloriesPer100g = 18,  ProteinPer100g = 0.9m,  CarbsPer100g = 3.9m,  FatPer100g = 0.2m, Source = FoodItemSource.Seed },
            [("quả vừa", 120), ("quả nhỏ", 80)]),

        (new FoodItem { NameVi = "Dưa leo",                  NameEn = "Cucumber",                    CaloriesPer100g = 15,  ProteinPer100g = 0.6m,  CarbsPer100g = 3.6m,  FatPer100g = 0.1m, Source = FoodItemSource.Seed },
            [("quả", 200), ("khẩu phần", 100)]),

        // ── Fruits ────────────────────────────────────────────────────────────
        (new FoodItem { NameVi = "Chuối",                    NameEn = "Banana",                      CaloriesPer100g = 89,  ProteinPer100g = 1.1m,  CarbsPer100g = 23.0m, FatPer100g = 0.3m, Source = FoodItemSource.Seed },
            [("quả vừa", 118), ("quả nhỏ", 80)]),

        (new FoodItem { NameVi = "Táo",                      NameEn = "Apple",                       CaloriesPer100g = 52,  ProteinPer100g = 0.3m,  CarbsPer100g = 14.0m, FatPer100g = 0.2m, Source = FoodItemSource.Seed },
            [("quả vừa", 182), ("quả nhỏ", 120)]),

        (new FoodItem { NameVi = "Xoài",                     NameEn = "Mango",                       CaloriesPer100g = 60,  ProteinPer100g = 0.8m,  CarbsPer100g = 15.0m, FatPer100g = 0.4m, Source = FoodItemSource.Seed },
            [("quả vừa", 200), ("chén", 165)]),

        (new FoodItem { NameVi = "Dưa hấu",                  NameEn = "Watermelon",                  CaloriesPer100g = 30,  ProteinPer100g = 0.6m,  CarbsPer100g = 7.5m,  FatPer100g = 0.2m, Source = FoodItemSource.Seed },
            [("lát", 280), ("chén", 150)]),

        // ── Dairy ─────────────────────────────────────────────────────────────
        (new FoodItem { NameVi = "Sữa bò tươi (không béo)",  NameEn = "Skim milk",                   CaloriesPer100g = 35,  ProteinPer100g = 3.4m,  CarbsPer100g = 5.1m,  FatPer100g = 0.1m, Source = FoodItemSource.Seed },
            [("ly", 240), ("hộp nhỏ", 180)]),

        (new FoodItem { NameVi = "Phô mai cottage",           NameEn = "Cottage cheese",              CaloriesPer100g = 98,  ProteinPer100g = 11.0m, CarbsPer100g = 3.4m,  FatPer100g = 4.3m, Source = FoodItemSource.Seed },
            [("chén", 226)]),

        // ── Vietnamese dishes ─────────────────────────────────────────────────
        (new FoodItem { NameVi = "Bún bò Huế",               NameEn = "Bun bo Hue (spicy beef noodle soup)", CaloriesPer100g = 80, ProteinPer100g = 5.5m, CarbsPer100g = 9.0m, FatPer100g = 2.5m, Source = FoodItemSource.Seed },
            [("tô", 500)]),

        (new FoodItem { NameVi = "Bánh mì thịt",             NameEn = "Banh mi (pork sandwich)",     CaloriesPer100g = 240, ProteinPer100g = 10.0m, CarbsPer100g = 30.0m, FatPer100g = 8.0m, Source = FoodItemSource.Seed },
            [("ổ", 180)]),

        (new FoodItem { NameVi = "Cơm tấm sườn",             NameEn = "Com tam (broken rice with pork chop)", CaloriesPer100g = 155, ProteinPer100g = 8.0m, CarbsPer100g = 22.0m, FatPer100g = 4.5m, Source = FoodItemSource.Seed },
            [("phần", 400)]),

        (new FoodItem { NameVi = "Gỏi cuốn (1 cuốn)",        NameEn = "Fresh spring roll (1 roll)",  CaloriesPer100g = 130, ProteinPer100g = 6.0m,  CarbsPer100g = 20.0m, FatPer100g = 2.5m, Source = FoodItemSource.Seed },
            [("cuốn", 75)]),

        (new FoodItem { NameVi = "Chả giò (chiên)",           NameEn = "Fried spring roll",           CaloriesPer100g = 250, ProteinPer100g = 7.0m,  CarbsPer100g = 22.0m, FatPer100g = 14.0m, Source = FoodItemSource.Seed },
            [("cuốn", 50)]),

        // ── Beverages ─────────────────────────────────────────────────────────
        (new FoodItem { NameVi = "Nước dừa",                  NameEn = "Coconut water",               CaloriesPer100g = 19,  ProteinPer100g = 0.7m,  CarbsPer100g = 3.7m,  FatPer100g = 0.2m, Source = FoodItemSource.Seed },
            [("trái", 240), ("ly", 300)]),

        (new FoodItem { NameVi = "Sữa đậu nành (không đường)", NameEn = "Soy milk (unsweetened)",   CaloriesPer100g = 33,  ProteinPer100g = 3.3m,  CarbsPer100g = 1.7m,  FatPer100g = 1.8m, Source = FoodItemSource.Seed },
            [("ly", 250), ("hộp", 200)]),
    ];
}

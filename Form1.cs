using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace StokTakipPortable
{
    public class Form1 : Form
    {
        private const string DataFileName = "data.json";
        private readonly string _dataPath;
        private AppData _data;

        // UI
        private TextBox txtDepot1 = new();
        private TextBox txtDepot2 = new();
        private TextBox txtDepot3 = new();
        private Button btnSaveDepots = new();

        private TextBox txtProductName = new();
        private ComboBox cmbUnit = new();
        private TextBox txtPrice = new();
        private Button btnAddProduct = new();
        private Button btnDeleteProduct = new();

        private TextBox txtSearch = new();
        private DataGridView gridStock = new();

        private ComboBox cmbDepot = new();
        private ComboBox cmbProduct = new();
        private TextBox txtQty = new();
        private Button btnIn = new();
        private Button btnOut = new();

        public Form1()
        {
            Text = "Stok Takip (3 Depo)";
            Width = 1100;
            Height = 680;
            StartPosition = FormStartPosition.CenterScreen;

            _dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DataFileName);
            _data = LoadData();

            BuildUi();
            RefreshAll();
        }

        // ---------- MODELS ----------
        public class AppData
        {
            public List<string> Depots { get; set; } = new(); // 3 depo adı
            public List<Product> Products { get; set; } = new();
            public List<StockRow> Stock { get; set; } = new(); // depo + ürün miktar
        }

        public class Product
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public string Name { get; set; } = "";
            public string Unit { get; set; } = "Adet"; // Adet / Metre
            public decimal Price { get; set; } = 0m;
        }

        public class StockRow
        {
            public string Depot { get; set; } = "";
            public Guid ProductId { get; set; }
            public decimal Qty { get; set; } = 0m;
        }

        // ---------- LOAD/SAVE ----------
        private AppData LoadData()
        {
            try
            {
                if (File.Exists(_dataPath))
                {
                    var json = File.ReadAllText(_dataPath);
                    var loaded = JsonSerializer.Deserialize<AppData>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (loaded != null)
                        return Normalize(loaded);
                }
            }
            catch { /* ignore */ }

            // İlk açılış
            return new AppData
            {
                Depots = new List<string> { "Depo 1", "Depo 2", "Depo 3" }
            };
        }

        private void SaveData()
        {
            _data = Normalize(_data);

            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_dataPath, json);
        }

        private AppData Normalize(AppData d)
        {
            if (d.Depots == null) d.Depots = new List<string>();
            if (d.Products == null) d.Products = new List<Product>();
            if (d.Stock == null) d.Stock = new List<StockRow>();

            // depo sayısını 3 yap
            while (d.Depots.Count < 3) d.Depots.Add($"Depo {d.Depots.Count + 1}");
            if (d.Depots.Count > 3) d.Depots = d.Depots.Take(3).ToList();

            // boş depo adı olmasın
            for (int i = 0; i < d.Depots.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(d.Depots[i]))
                    d.Depots[i] = $"Depo {i + 1}";
                d.Depots[i] = d.Depots[i].Trim();
            }

            // Ürün adı trim
            foreach (var p in d.Products)
            {
                p.Name = (p.Name ?? "").Trim();
                p.Unit = string.IsNullOrWhiteSpace(p.Unit) ? "Adet" : p.Unit.Trim();
            }

            // Geçersiz stok satırlarını temizle (silinmiş ürünler)
            var set = new HashSet<Guid>(d.Products.Select(x => x.Id));
            d.Stock = d.Stock.Where(s => set.Contains(s.ProductId) && !string.IsNullOrWhiteSpace(s.Depot)).ToList();

            return d;
        }

        // ---------- UI ----------
        private void BuildUi()
        {
            var tabs = new TabControl { Dock = DockStyle.Fill };
            Controls.Add(tabs);

            var tab1 = new TabPage("Depolar + Ürünler");
            var tab2 = new TabPage("Stok Giriş/Çıkış");
            var tab3 = new TabPage("Stok Liste + Arama");
            tabs.TabPages.Add(tab1);
            tabs.TabPages.Add(tab2);
            tabs.TabPages.Add(tab3);

            BuildTabDepoUrun(tab1);
            BuildTabHareket(tab2);
            BuildTabStockList(tab3);
        }

        private void BuildTabDepoUrun(TabPage tab)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            tab.Controls.Add(panel);

            var grpDepo = new GroupBox { Text = "3 Depo İsmi", Dock = DockStyle.Top, Height = 120 };
            panel.Controls.Add(grpDepo);

            txtDepot1.SetBounds(20, 30, 220, 28);
            txtDepot2.SetBounds(260, 30, 220, 28);
            txtDepot3.SetBounds(500, 30, 220, 28);
            btnSaveDepots.Text = "Depo İsimlerini Kaydet";
            btnSaveDepots.SetBounds(740, 30, 220, 30);
            btnSaveDepots.Click += (_, __) => SaveDepots();

            grpDepo.Controls.AddRange(new Control[] { txtDepot1, txtDepot2, txtDepot3, btnSaveDepots });

            var grpUrun = new GroupBox { Text = "Ürün Ekle / Sil", Dock = DockStyle.Fill };
            panel.Controls.Add(grpUrun);

            var lblName = new Label { Text = "Ürün Adı:", AutoSize = true };
            var lblUnit = new Label { Text = "Birim:", AutoSize = true };
            var lblPrice = new Label { Text = "Fiyat (₺):", AutoSize = true };

            lblName.SetBounds(20, 40, 80, 20);
            txtProductName.SetBounds(110, 35, 260, 28);

            lblUnit.SetBounds(390, 40, 60, 20);
            cmbUnit.SetBounds(450, 35, 120, 28);
            cmbUnit.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbUnit.Items.AddRange(new object[] { "Adet", "Metre" });
            cmbUnit.SelectedIndex = 0;

            lblPrice.SetBounds(590, 40, 80, 20);
            txtPrice.SetBounds(680, 35, 120, 28);

            btnAddProduct.Text = "Ürün Ekle";
            btnAddProduct.SetBounds(820, 35, 140, 30);
            btnAddProduct.Click += (_, __) => AddProduct();

            btnDeleteProduct.Text = "Seçili Ürünü Sil";
            btnDeleteProduct.SetBounds(820, 75, 140, 30);
            btnDeleteProduct.Click += (_, __) => DeleteSelectedProduct();

            var list = new ListBox { Name = "lstProducts" };
            list.SetBounds(20, 80, 780, 430);

            grpUrun.Controls.AddRange(new Control[]
            {
                lblName, txtProductName, lblUnit, cmbUnit, lblPrice, txtPrice, btnAddProduct, btnDeleteProduct, list
            });
        }

        private void BuildTabHareket(TabPage tab)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            tab.Controls.Add(panel);

            var grp = new GroupBox { Text = "Stok İşlemi", Dock = DockStyle.Top, Height = 160 };
            panel.Controls.Add(grp);

            var lblDepo = new Label { Text = "Depo:", AutoSize = true };
            var lblUrun = new Label { Text = "Ürün:", AutoSize = true };
            var lblQty = new Label { Text = "Miktar:", AutoSize = true };

            lblDepo.SetBounds(20, 40, 60, 20);
            cmbDepot.SetBounds(90, 35, 240, 28);
            cmbDepot.DropDownStyle = ComboBoxStyle.DropDownList;

            lblUrun.SetBounds(350, 40, 60, 20);
            cmbProduct.SetBounds(410, 35, 360, 28);
            cmbProduct.DropDownStyle = ComboBoxStyle.DropDownList;

            lblQty.SetBounds(790, 40, 60, 20);
            txtQty.SetBounds(850, 35, 120, 28);

            btnIn.Text = "Stok Ekle (Giriş)";
            btnIn.SetBounds(90, 85, 220, 32);
            btnIn.Click += (_, __) => StockMove(isIn: true);

            btnOut.Text = "Stok Düş (Çıkış)";
            btnOut.SetBounds(330, 85, 220, 32);
            btnOut.Click += (_, __) => StockMove(isIn: false);

            grp.Controls.AddRange(new Control[]
            {
                lblDepo, cmbDepot, lblUrun, cmbProduct, lblQty, txtQty, btnIn, btnOut
            });

            var note = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Not: Aynı ürün aynı depoda tekrar giriş yapılırsa, stok üstüne eklenir. Çıkışta stok yetmezse düşürmez.",
                Padding = new Padding(10),
                AutoSize = false
            };
            panel.Controls.Add(note);
        }

        private void BuildTabStockList(TabPage tab)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            tab.Controls.Add(panel);

            var top = new Panel { Dock = DockStyle.Top, Height = 45 };
            panel.Controls.Add(top);

            var lbl = new Label { Text = "Arama (Ürün adı):", AutoSize = true };
            lbl.SetBounds(10, 12, 120, 20);
            txtSearch.SetBounds(140, 8, 320, 28);
            txtSearch.TextChanged += (_, __) => RefreshStockGrid();

            top.Controls.AddRange(new Control[] { lbl, txtSearch });

            gridStock.Dock = DockStyle.Fill;
            gridStock.ReadOnly = true;
            gridStock.AllowUserToAddRows = false;
            gridStock.AllowUserToDeleteRows = false;
            gridStock.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridStock.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            panel.Controls.Add(gridStock);
        }

        // ---------- ACTIONS ----------
        private void SaveDepots()
        {
            var d1 = (txtDepot1.Text ?? "").Trim();
            var d2 = (txtDepot2.Text ?? "").Trim();
            var d3 = (txtDepot3.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(d1) || string.IsNullOrWhiteSpace(d2) || string.IsNullOrWhiteSpace(d3))
            {
                MessageBox.Show("3 depo ismi de dolu olmalı.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _data.Depots = new List<string> { d1, d2, d3 };
            SaveData();
            RefreshAll();
            MessageBox.Show("Depo isimleri kaydedildi.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AddProduct()
        {
            var name = (txtProductName.Text ?? "").Trim();
            var unit = (cmbUnit.SelectedItem?.ToString() ?? "Adet").Trim();
            var priceText = (txtPrice.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Ürün adı boş olamaz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!TryParseDecimal(priceText, out var price) || price < 0)
            {
                MessageBox.Show("Fiyat sayı olmalı (örn: 25 veya 25,50).", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Aynı isim+birim varsa tekrar ekleme (ürün kartı)
            if (_data.Products.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) &&
                                        string.Equals(p.Unit, unit, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Bu ürün zaten var (aynı isim ve birim).", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _data.Products.Add(new Product
            {
                Id = Guid.NewGuid(),
                Name = name,
                Unit = unit,
                Price = price
            });

            SaveData();
            txtProductName.Text = "";
            txtPrice.Text = "";
            cmbUnit.SelectedIndex = 0;

            RefreshAll();
        }

        private void DeleteSelectedProduct()
        {
            // Tab1'deki ListBox'ı bul
            var lst = FindControlRecursive<ListBox>(this, "lstProducts");
            if (lst == null || lst.SelectedItem == null)
            {
                MessageBox.Show("Silmek için listeden ürün seç.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (lst.SelectedItem is not ProductView pv) return;

            var ok = MessageBox.Show($"Ürün silinsin mi?\n\n{pv.Name}", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (ok != DialogResult.Yes) return;

            _data.Products = _data.Products.Where(p => p.Id != pv.Id).ToList();
            _data.Stock = _data.Stock.Where(s => s.ProductId != pv.Id).ToList();

            SaveData();
            RefreshAll();
        }

        private void StockMove(bool isIn)
        {
            if (cmbDepot.SelectedItem == null || cmbProduct.SelectedItem == null)
            {
                MessageBox.Show("Depo ve ürün seç.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var depot = cmbDepot.SelectedItem.ToString() ?? "";
            if (cmbProduct.SelectedItem is not ProductView pv)
                return;

            var qtyText = (txtQty.Text ?? "").Trim();
            if (!TryParseDecimal(qtyText, out var qty) || qty <= 0)
            {
                MessageBox.Show("Miktar sayı olmalı (adet için 1, metre için 2,5 gibi).", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row = _data.Stock.FirstOrDefault(s => s.Depot == depot && s.ProductId == pv.Id);
            if (row == null)
            {
                row = new StockRow { Depot = depot, ProductId = pv.Id, Qty = 0m };
                _data.Stock.Add(row);
            }

            if (isIn)
            {
                row.Qty += qty; // üstüne ekleme
            }
            else
            {
                if (row.Qty < qty)
                {
                    MessageBox.Show($"Yetersiz stok! Mevcut: {FormatQty(row.Qty, pv.Unit)}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                row.Qty -= qty;
            }

            SaveData();
            txtQty.Text = "";
            RefreshAll();
        }

        // ---------- REFRESH ----------
        private void RefreshAll()
        {
            // Depo textbox
            txtDepot1.Text = _data.Depots.ElementAtOrDefault(0) ?? "Depo 1";
            txtDepot2.Text = _data.Depots.ElementAtOrDefault(1) ?? "Depo 2";
            txtDepot3.Text = _data.Depots.ElementAtOrDefault(2) ?? "Depo 3";

            // Depo combo
            cmbDepot.Items.Clear();
            foreach (var d in _data.Depots)
                cmbDepot.Items.Add(d);
            if (cmbDepot.Items.Count > 0) cmbDepot.SelectedIndex = 0;

            // Ürün listesi + combo
            var productViews = _data.Products
                .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(p => new ProductView(p))
                .ToList();

            var lst = FindControlRecursive<ListBox>(this, "lstProducts");
            if (lst != null)
            {
                lst.DataSource = null;
                lst.DisplayMember = "Name";
                lst.DataSource = productViews;
            }

            cmbProduct.DataSource = null;
            cmbProduct.DisplayMember = "Name";
            cmbProduct.DataSource = productViews;

            RefreshStockGrid();
        }

        private void RefreshStockGrid()
        {
            var q = (txtSearch.Text ?? "").Trim();

            var rows = new List<StockGridRow>();

            foreach (var p in _data.Products)
            {
                foreach (var d in _data.Depots)
                {
                    var stock = _data.Stock.FirstOrDefault(s => s.Depot == d && s.ProductId == p.Id)?.Qty ?? 0m;
                    rows.Add(new StockGridRow
                    {
                        ProductName = p.Name,
                        Depot = d,
                        Qty = FormatQty(stock, p.Unit),
                        Unit = p.Unit,
                        Price = p.Price.ToString("0.##", CultureInfo.GetCultureInfo("tr-TR"))
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                rows = rows
                    .Where(r => r.ProductName.IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    .ToList();
            }

            gridStock.DataSource = rows;
        }

        // ---------- HELPERS ----------
        private static bool TryParseDecimal(string input, out decimal value)
        {
            // TR: virgül, nokta kabul et
            input = (input ?? "").Trim();
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out value))
                return true;
            if (decimal.TryParse(input.Replace('.', ','), NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out value))
                return true;
            return decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private static string FormatQty(decimal qty, string unit)
        {
            // Adet genelde tam sayı gibi; metre virgüllü olabilir
            if (string.Equals(unit, "Adet", StringComparison.OrdinalIgnoreCase))
                return qty.ToString("0.##", CultureInfo.GetCultureInfo("tr-TR"));
            return qty.ToString("0.###", CultureInfo.GetCultureInfo("tr-TR"));
        }

        private static T? FindControlRecursive<T>(Control root, string name) where T : Control
        {
            foreach (Control c in root.Controls)
            {
                if (c is T t && c.Name == name) return t;
                var child = FindControlRecursive<T>(c, name);
                if (child != null) return child;
            }
            return null;
        }

        private class ProductView
        {
            public Guid Id { get; }
            public string Name { get; }
            public string Unit { get; }
            public decimal Price { get; }

            public ProductView(Product p)
            {
                Id = p.Id;
                Unit = p.Unit;
                Price = p.Price;
                Name = $"{p.Name}  ({p.Unit})  -  ₺{p.Price.ToString("0.##", CultureInfo.GetCultureInfo("tr-TR"))}";
            }
        }

        private class StockGridRow
        {
            public string ProductName { get; set; } = "";
            public string Depot { get; set; } = "";
            public string Qty { get; set; } = "";
            public string Unit { get; set; } = "";
            public string Price { get; set; } = "";
        }
    }
}

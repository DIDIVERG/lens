using System;
using System.Data;
using System.Windows.Forms;
using System.Xml;
using Ydb.Sdk;
using Ydb.Sdk.Auth;
using Ydb.Sdk.Services.Table;
using Ydb.Sdk.Value;

namespace OnlineStore
{
    public partial class Form1 : Form
    {

        private string id = "";
        private int intRow = 0;
        private string nameTable = "Покупатели";
        private string connectNameTable = "Customers";
        private Driver driver;
        private DataTable dt;

        public Form1()
        {
            InitializeComponent();
            resetMe();
            InitDB();

        }

        private async Task InitDB()
        {
            var endpoint = "grpcs://ydb.serverless.yandexcloud.net:2135";
            var database = "/ru-central1/b1gdr3lhj3btpt3e9nm7/etn23rrimb9nannj4v24";
            var token = "t1.9euelZqNyJ2UypiRk5yclJPJzZWanu3rnpWayJSSi5SZzJmYnsuPmJuKj8vl8_ctRS1Q-e9LBGYj_N3z921zKlD570sEZiP8zef1656VmoyZipvKnY-dmJ2VkJqUyouL7_zF656VmoyZipvKnY-dmJ2VkJqUyouL.P20xuiouLO9HoKrzz7NIXe0vwuzJwNjd9c9teP_DF1snuoCZE8RbDroTLAqihGi3OlOrUMtv_5ByFerCnbXiBA";

            var config = new DriverConfig(
                endpoint: endpoint,
                database: database,
                credentials: new TokenProvider(token)
            );

            driver = new Driver(
                config: config
            );

            await driver.Initialize();
            loadData();
        }

        private void resetMe()
        {
            id = string.Empty;

            firstNameTextBox.Text = "";
            lastNameTextBox.Text = "";
            middleNameTextBox.Text = "";
            nameTextBox.Text = "";


            customerUpdateButton.Text = "Изменить ()";
            customerDeleteButton.Text = "Удалить ()";
            productUpdateButton.Text = "Изменить ()";
            productDeleteButton.Text = "Удалить ()";
        }

        private async Task loadData()
        {

            var tableClient = new TableClient(driver, new TableClientConfig());

            switch (nameTable)
            {
                case "Покупатели":
                    connectNameTable = "Customers";
                    break;
                case "Товары":
                    connectNameTable = "Products";
                    break;
                case "Заказы":
                    connectNameTable = "Orders";
                    break;
                case "Заказанные товары":
                    connectNameTable = "OrdersProducts";
                    break;
                default:
                    break;
            }

            var response = await tableClient.SessionExec(async session =>
            {
                var query = @$"SELECT * FROM {connectNameTable}";

                return await session.ExecuteDataQuery(
                query: query,
                txControl: TxControl.BeginSerializableRW().Commit()
                );
            });
            response.Status.EnsureSuccess();
            var queryResponse = (ExecuteDataQueryResponse)response;
            var resultSet = queryResponse.Result.ResultSets[0];

            dt = new DataTable();

            switch (nameTable)
            {
                case "Покупатели":
                    dt.Columns.Add("Id", typeof(ulong));
                    dt.Columns.Add("LastName", typeof(string));
                    dt.Columns.Add("FirstName", typeof(string));
                    dt.Columns.Add("MiddleName", typeof(string));
                    foreach (var row in resultSet.Rows)
                    {
                        dt.Rows.Add((ulong?)row["Id"], (string?)row["LastName"],
                            (string?)row["FirstName"], (string?)row["MiddleName"]);
                    }
                    break;
                case "Товары":
                    dt.Columns.Add("Id", typeof(ulong));
                    dt.Columns.Add("Name", typeof(string));
                    dt.Columns.Add("Price", typeof(ulong));
                    foreach (var row in resultSet.Rows)
                    {
                        dt.Rows.Add((ulong?)row["Id"], (string?)row["Name"], (ulong?)row["Price"]);
                    }
                    break;
                case "Заказы":
                    dt.Columns.Add("Id", typeof(ulong));
                    dt.Columns.Add("Condition", typeof(string));
                    dt.Columns.Add("Price", typeof(ulong));
                    foreach (var row in resultSet.Rows)
                    {
                        dt.Rows.Add((ulong?)row["Id"], (string?)row["Condition"],
                            (ulong?)row["Price"]);

                    }
                    break;
                case "Заказанные товары":
                    dt.Columns.Add("OrderId", typeof(ulong));
                    dt.Columns.Add("ProductId", typeof(ulong));
                    dt.Columns.Add("Price", typeof(ulong));
                    dt.Columns.Add("Quantity", typeof(ulong));
                    foreach (var row in resultSet.Rows)
                    {
                        dt.Rows.Add((ulong?)row["OrderId"], (ulong?)row["ProductId"],
                            (ulong?)row["PriceProducts"], (ulong?)row["QuantityProduct"]);
                    }
                    break;
                default:
                    break;
            }

            if (dt.Rows.Count > 0)
            {
                intRow = Convert.ToInt32(dt.Rows.Count.ToString());
            }
            else
            {
                intRow = 0;
            }

            toolStripStatusLabel1.Text = "Number of row(s): " + intRow.ToString();

            DataGridView dgv1 = dataGridView1;

            dgv1.MultiSelect = false;
            dgv1.AutoGenerateColumns = true;
            dgv1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgv1.DataSource = dt;

            dgv1.Columns[0].Width = 55;
        }

        private ulong GetLastIdFromTable()
        {
            ulong maxId = 0;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if ((ulong)dt.Rows[i]["Id"] > maxId)
                    maxId = (ulong)dt.Rows[i]["Id"];
            }
            return maxId + 1;
        }

        private async void customerInsertButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(firstNameTextBox.Text.Trim())
                || string.IsNullOrEmpty(lastNameTextBox.Text.Trim())
                || string.IsNullOrEmpty(middleNameTextBox.Text.Trim()))
            {
                MessageBox.Show("Пожалуйста введите ФИО полностью.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            var tableClient = new TableClient(driver, new TableClientConfig());

            var response = await tableClient.SessionExec(async session =>
            {
                var query = @"
                    DECLARE $Id AS Uint64;
                    DECLARE $FirstName AS Utf8;
                    DECLARE $LastName AS Utf8;
                    DECLARE $MiddleName AS Utf8;

                    UPSERT INTO Customers (Id, FirstName, LastName, MiddleName) VALUES
                        ($Id, $FirstName, $LastName, $MiddleName);
                ";

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters: new Dictionary<string, YdbValue>
                        {
                { "$Id", YdbValue.MakeUint64(GetLastIdFromTable()) },
                { "$FirstName", YdbValue.MakeUtf8(firstNameTextBox.Text.Trim()) },
                { "$LastName", YdbValue.MakeUtf8(lastNameTextBox.Text.Trim()) },
                { "$MiddleName", YdbValue.MakeUtf8(middleNameTextBox.Text.Trim()) },
                        }
                );
            });

            response.Status.EnsureSuccess();
            if (response.Status.StatusCode == StatusCode.Success)
                MessageBox.Show("Данные сохранены.", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            loadData();
            resetMe();
        }

        private async void customerUpdateButton_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                return;
            }

            if (string.IsNullOrEmpty(this.id))
            {
                MessageBox.Show("Пожалуйста выберите элемент из списка.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (string.IsNullOrEmpty(lastNameTextBox.Text.Trim())
                || string.IsNullOrEmpty(firstNameTextBox.Text.Trim()) ||
                string.IsNullOrEmpty(middleNameTextBox.Text.Trim()))
            {
                MessageBox.Show("Пожалуйста введите ФИО полностью.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            var tableClient = new TableClient(driver, new TableClientConfig());

            var response = await tableClient.SessionExec(async session =>
            {
                var query = @"
                    DECLARE $Id AS Uint64;
                    DECLARE $FirstName AS Utf8;
                    DECLARE $LastName AS Utf8;
                    DECLARE $MiddleName AS Utf8;

                    UPSERT INTO Customers (Id, FirstName, LastName, MiddleName) VALUES
                        ($Id, $FirstName, $LastName, $MiddleName);
                ";

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters: new Dictionary<string, YdbValue>
                        {
                { "$Id", YdbValue.MakeUint64(ulong.Parse(this.id)) },
                { "$FirstName", YdbValue.MakeUtf8(firstNameTextBox.Text.Trim()) },
                { "$LastName", YdbValue.MakeUtf8(lastNameTextBox.Text.Trim()) },
                { "$MiddleName", YdbValue.MakeUtf8(middleNameTextBox.Text.Trim()) },
                        }
                );
            });

            response.Status.EnsureSuccess();
            if (response.Status.StatusCode == StatusCode.Success)
                MessageBox.Show("Данные успешно сохранены.", "Успех",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            loadData();
            resetMe();
        }

        private async void deleteButton_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                return;
            }

            if (string.IsNullOrEmpty(this.id))
            {
                MessageBox.Show("Пожалуйста выберите элемент из списка.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (MessageBox.Show("Вы уверены что хотите удалить данный элемент?", "Удаление данных",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
            {
                var tableClient = new TableClient(driver, new TableClientConfig());


                var response = await tableClient.SessionExec(async session =>
                {
                    var query = @$"
                        DECLARE $Id AS Uint64;        

                        DELETE FROM {connectNameTable} WHERE Id == $Id;
                    ";

                    return await session.ExecuteDataQuery(
                        query: query,
                        txControl: TxControl.BeginSerializableRW().Commit(),
                        parameters: new Dictionary<string, YdbValue>
                            {
                { "$Id", YdbValue.MakeUint64(ulong.Parse(this.id)) }
                            }
                    );
                });
                response.Status.EnsureSuccess();
                if (response.Status.StatusCode == StatusCode.Success)
                    MessageBox.Show("Элемент был удален.", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                loadData();
                resetMe();
            }

        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {

            if (e.RowIndex != -1)
            {
                DataGridView dgv1 = dataGridView1;

                this.id = Convert.ToString(dgv1.CurrentRow.Cells[0].Value);
                customerUpdateButton.Text = "Изменить (" + this.id + ")";
                customerDeleteButton.Text = "Удалить (" + this.id + ")";
                productUpdateButton.Text = "Изменить (" + this.id + ")";
                productDeleteButton.Text = "Удалить (" + this.id + ")";
            }

        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            nameTable = tabControl1.TabPages[tabControl1.SelectedIndex].Text;
            loadData();
            resetMe();
        }

        private async void productInsertButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(nameTextBox.Text.Trim()))
            {
                MessageBox.Show("Пожалуйста введите название продукта", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            var tableClient = new TableClient(driver, new TableClientConfig());

            var response = await tableClient.SessionExec(async session =>
            {
                var query = @"
                    DECLARE $Id AS Uint64;
                    DECLARE $Name AS Utf8;
                    DECLARE $Price AS Uint64;

                    UPSERT INTO Products (Id, Name,Price) VALUES
                        ($Id, $Name,$Price);
                ";

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters: new Dictionary<string, YdbValue>
                        {
                { "$Id", YdbValue.MakeUint64(GetLastIdFromTable()) },
                { "$Name", YdbValue.MakeUtf8(nameTextBox.Text.Trim()) },
                 {"$Price",YdbValue.MakeUint64(Convert.ToUInt64(priceTextBox.Text.Trim())) },

                        }
                );
            });
            response.Status.EnsureSuccess();
            if (response.Status.StatusCode == StatusCode.Success)
                MessageBox.Show("Данные сохранены", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

            loadData();
            resetMe();
        }

        private async void productUpdateButton_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                return;
            }

            if (string.IsNullOrEmpty(this.id))
            {
                MessageBox.Show("Пожалуйста выберите элемент из списка", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (string.IsNullOrEmpty(nameTextBox.Text.Trim()))
            {
                MessageBox.Show("Пожалуйста введите название.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            var tableClient = new TableClient(driver, new TableClientConfig());

            var response = await tableClient.SessionExec(async session =>
            {
                var query = @"
                    DECLARE $Id AS Uint64;
                    DECLARE $Name AS Utf8;
                    DECLARE $Price AS Uint64;

                    UPSERT INTO Products (Id, Name,Price) VALUES
                        ($Id, $Name,$Price);
                ";

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters: new Dictionary<string, YdbValue>
                        {
                { "$Id", YdbValue.MakeUint64(ulong.Parse(this.id)) },
                { "$Name", YdbValue.MakeUtf8(nameTextBox.Text.Trim()) },
                 {"$Price",YdbValue.MakeUint64(Convert.ToUInt64(priceTextBox.Text.Trim())) },
                        }
                );
            });
            response.Status.EnsureSuccess();
            if (response.Status.StatusCode == StatusCode.Success)
                MessageBox.Show("Данные сохранены.", "Успех",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            loadData();
            resetMe();
        }

        private async void ReportWorker_Click(object sender, EventArgs e)
        {
            dt.Clear();
            dt = new DataTable();
            var tableClient = new TableClient(driver, new TableClientConfig());
     
            var response = await tableClient.SessionExec(async session =>
            {
                var query = @$"SELECT * FROM CustomersReport";

                return await session.ExecuteDataQuery(
                query: query,
                txControl: TxControl.BeginSerializableRW().Commit()
                );
            });
            response.Status.EnsureSuccess();
            var queryResponse = (ExecuteDataQueryResponse)response;
            var resultSet = queryResponse.Result.ResultSets[0];



            dt.Columns.Add("Id", typeof(ulong));
            dt.Columns.Add("LastName", typeof(string));
            dt.Columns.Add("FirstName", typeof(string));
            dt.Columns.Add("MiddleName", typeof(string));
            dt.Columns.Add("Payed", typeof(ulong));
            dt.Columns.Add("Delivered", typeof(ulong));
            dt.Columns.Add("Cancelled", typeof(ulong));
            foreach (var row in resultSet.Rows)
            {
                dt.Rows.Add((ulong?)row["Id"], (string?)row["LastName"],
                    (string?)row["FirstName"], (string?)row["MiddleName"],
                    (ulong?)row["Payed"], (ulong?)row["Delivered"], (ulong?)row["Cancelled"]);
            }

            DataGridView dgv1 = dataGridView1;

            dgv1.MultiSelect = false;
            dgv1.AutoGenerateColumns = true;
            dgv1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgv1.DataSource = dt;

            dgv1.Columns[0].Width = 55;
        }

        private async void ReportProduct_Click(object sender, EventArgs e)
        {
            dt.Clear();
            dt = new DataTable();
            var tableClient = new TableClient(driver, new TableClientConfig());
       
            var response = await tableClient.SessionExec(async session =>
            {
                var query = @$"SELECT * FROM ProductsReport";

                return await session.ExecuteDataQuery(
                query: query,
                txControl: TxControl.BeginSerializableRW().Commit()
                );
            });
            response.Status.EnsureSuccess();
            var queryResponse = (ExecuteDataQueryResponse)response;
            var resultSet = queryResponse.Result.ResultSets[0];



            dt.Columns.Add("Id", typeof(ulong));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("PriceProducts", typeof(ulong));
            dt.Columns.Add("QuantityProduct", typeof(ulong));
            foreach (var row in resultSet.Rows)
            {
                dt.Rows.Add((ulong?)row["Id"], (string?)row["Name"], (ulong?)row["PriceProducts"],(ulong?)row["QuantityProduct"]);
            }

            DataGridView dgv1 = dataGridView1;

            dgv1.MultiSelect = false;
            dgv1.AutoGenerateColumns = true;
            dgv1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgv1.DataSource = dt;

            dgv1.Columns[0].Width = 55;
        }
    }
}
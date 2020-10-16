using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Atomus.Service;
using Atomus.Control.Dictionary.Controllers;
using Atomus.Control.Dictionary.Models;
using Atomus.Diagnostics;
using DevExpress.XtraEditors;

namespace Atomus.Control.Dictionary
{
    public class DevExpressControl : IDictionary, IAction
    {
        string IDictionary.Code { get; set; }
        System.Windows.Forms.Control[] IDictionary.Controls { get; set; }
        System.Windows.Forms.Control IDictionary.CurrentControl { get; set; }

        bool IDictionary.AutoComplete { get; set; }
        BeforeAction IDictionary.BeforeAction { get; set; }
        AfterAction IDictionary.AfterAction { get; set; }
        bool IDictionary.WaterMark { get; set; }

        static IDictionaryForm dictionaryForm;
        IDictionaryForm IDictionary.DictionaryForm
        {
            get
            {
                return dictionaryForm;
            }
            set
            {
                dictionaryForm = value;
            }
        }

        Size IDictionary.DictionaryFormSize { get; set; }

        string IBeforeEventArgs.Where { get; set; }
        bool IBeforeEventArgs.SearchAll { get; set; }
        bool IBeforeEventArgs.StartsWith { get; set; }

        DataRow IAfterEventArgs.DataRow { get; set; }
        DataTable IAfterEventArgs.DataTable { get; set; }

        private AtomusControlEventHandler beforeActionEventHandler;
        private AtomusControlEventHandler afterActionEventHandler;

        private static Dictionary<string, List<string>> listDictionaryNames;
        private static string buttonText;

        private ICore ParentCore { get; set; }

        #region Init
        static DevExpressControl()
        {
            listDictionaryNames = new Dictionary<string, List<string>>();

            buttonText = "";
        }

        public DevExpressControl()
        {
            //Form 생성하기)
            if (((IDictionary)this).DictionaryForm == null)
                ((IDictionary)this).DictionaryForm = this.CreateDictionaryForm();

            //ButtonText 지정
            if (buttonText == "")
                buttonText = this.GetAttribute("ButtonText");
        }

        public DevExpressControl(object Object) : this()
        {
            if (Object != null && Object is ICore)
                this.ParentCore = (ICore)Object;
        }

        private IDictionaryForm CreateDictionaryForm()
        {
            //DictionaryForm 생성
            try
            {
                return (IDictionaryForm)this.CreateInstance("Form");//Form 생성하기)
                //return (IDictionaryForm)new DevExpressXtraForm();
            }
            catch (AtomusException exception)
            {
                throw exception;
            }
            catch (Exception exception)
            {
                throw new AtomusException(exception);
            }
        }
        private string GetButtonText()
        {
            //ButtonText 지정
            try
            {
                return this.GetAttribute("ButtonText") ?? "";
            }
            catch (AtomusException exception)
            {
                throw exception;
            }
            catch (Exception exception)
            {
                throw new AtomusException(exception);
            }
        }
        #endregion

        #region Dictionary
        #endregion

        #region Spread
        #endregion

        #region IO
        object IAction.ControlAction(ICore sender, AtomusControlArgs e)
        {
            IDictionary dictionary;
            XtraForm form;
            IDictionaryForm dictionaryForm;

            try
            {
                this.beforeActionEventHandler?.Invoke(this, e);

                switch (e.Action)
                {
                    case "Search":
                        return this.Search(this);

                    case "SearchAsync":
                        return this.SearchAsync(this);

                    case "SetResult":
                        SetResult((IDictionary)sender);
                        return true;

                    case "Show":
                        dictionary = (IDictionary)e.Value;

                        if (dictionary.BeforeAction != null && !dictionary.BeforeAction.Invoke(dictionary.CurrentControl, (IBeforeEventArgs)dictionary))
                            return false;

                        dictionaryForm = dictionary.DictionaryForm;
                        dictionaryForm.Dictionary = dictionary;

                        form = (XtraForm)dictionaryForm;
                        form.Location = System.Windows.Forms.Control.MousePosition;
                        //form.Name = listDictionaryNames[dictionary.Code][ControlIndex(dictionary)];

                        form.Visible = true;

                        return true;

                    default:
                        throw new AtomusException("'{0}'은 처리할 수 없는 Action 입니다.".Translate(e.Action));
                }
            }
            finally
            {
                this.afterActionEventHandler?.Invoke(this, e);
            }
        }

        private bool Search(IDictionary dictionary)
        {
            IResponse result;

            try
            {
                result = this.Search(new DevExpressControlSearchModel()
                {
                    CODE = dictionary.Code,
                    SEARCH_TEXT = dictionary.CurrentControl != null && dictionary.CurrentControl.Text != null ? dictionary.CurrentControl.Text.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") : "",
                    SEARCH_INDEX = ControlIndex(dictionary) + 1,
                    COND_ETC = dictionary.Where,
                    SEARCH_ALL = (dictionary.SearchAll) ? "Y" : "N",
                    STARTS_WITH = (dictionary.StartsWith) ? "Y" : "N"
                }, this.ParentCore);

                if (result.Status == Status.OK)
                {
                    if (result.DataSet.Tables.Count >= 1)
                    {
                        if (!listDictionaryNames.ContainsKey(dictionary.Code))
                        {
                            List<string> _Names = new List<string>();
                            foreach (DataColumn _DataColumn in result.DataSet.Tables[0].Columns)
                            {
                                if (_DataColumn.Caption.Contains('^'))
                                    _Names.Add(_DataColumn.Caption.Split('^')[0].Translate());
                            }
                            listDictionaryNames.Add(dictionary.Code, _Names);
                        }

                        dictionary.DataTable = result.DataSet.Tables[0];
                        dictionary.DataTable.TableName = result.DataSet.Tables[1].Rows[0]["DESCRIPTION"].ToString().Translate();

                        if (result.DataSet.Tables[0].Rows.Count == 1)
                            dictionary.DataRow = result.DataSet.Tables[0].Rows[0];

                        return true;
                    }

                    return false;
                }
                else
                    throw new AtomusException(result.Message);
            }
            catch (Exception exception)
            {
                this.MessageBoxShow(exception);
                return false;
            }
            finally
            {
            }
        }
        private async Task<bool> SearchAsync(IDictionary dictionary)
        {
            IResponse result;

            try
            {
                result = await this.SearchAsync(new DevExpressControlSearchModel()
                {
                    CODE = dictionary.Code,
                    SEARCH_TEXT = dictionary.CurrentControl != null && dictionary.CurrentControl.Text != null ? dictionary.CurrentControl.Text.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") : "",
                    SEARCH_INDEX = ControlIndex(dictionary) + 1,
                    COND_ETC = dictionary.Where,
                    SEARCH_ALL = (dictionary.SearchAll) ? "Y" : "N",
                    STARTS_WITH = (dictionary.StartsWith) ? "Y" : "N"
                }, this.ParentCore);

                if (result.Status == Status.OK)
                {
                    if (result.DataSet.Tables.Count >= 1)
                    {
                        if (!listDictionaryNames.ContainsKey(dictionary.Code))
                        {
                            List<string> _Names = new List<string>();
                            foreach (DataColumn _DataColumn in result.DataSet.Tables[0].Columns)
                            {
                                if (_DataColumn.Caption.Contains('^'))
                                    _Names.Add(_DataColumn.Caption.Split('^')[0].Translate());
                            }
                            listDictionaryNames.Add(dictionary.Code, _Names);
                        }

                        dictionary.DataTable = result.DataSet.Tables[0];
                        dictionary.DataTable.TableName = result.DataSet.Tables[1].Rows[0]["DESCRIPTION"].ToString().Translate();

                        if (result.DataSet.Tables[0].Rows.Count == 1)
                            dictionary.DataRow = result.DataSet.Tables[0].Rows[0];
                        else
                            dictionary.DataRow = null;

                        return true;
                    }

                    return false;
                }
                else
                    throw new AtomusException(result.Message);
            }
            catch (Exception exception)
            {
                this.MessageBoxShow(exception);
                return false;
            }
            finally
            {
            }
        }
        #endregion

        #region Event
        event AtomusControlEventHandler IAction.BeforeActionEventHandler
        {
            add
            {
                this.beforeActionEventHandler += value;
            }
            remove
            {
                this.beforeActionEventHandler -= value;
            }
        }
        event AtomusControlEventHandler IAction.AfterActionEventHandler
        {
            add
            {
                this.afterActionEventHandler += value;
            }
            remove
            {
                this.afterActionEventHandler -= value;
            }
        }

        /// <summary>
        /// 컨트롤이 입력되면 발생합니다.
        /// 라벨을 숨기고 버튼을 보이게 한다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Event_Control_Enter(object sender, EventArgs e)
        {
            System.Windows.Forms.Control control;
            IDictionary dictionary;

            try
            {
                if (sender is System.Windows.Forms.Control)//Windows Control 일 경우
                    control = (System.Windows.Forms.Control)sender;
                else
                    return;

                if (control.Tag is IDictionary) //IDictionary가 있으면
                {
                    foreach (System.Windows.Forms.Control _Con in control.Controls)
                    {
                        if (_Con is LabelControl)//라벨은 숨긴다
                            _Con.Visible = false;

                        if (_Con is SimpleButton)//버튼은 보인다
                            _Con.Visible = true;
                    }

                    dictionary = (IDictionary)control.Tag;
                    dictionary.CurrentControl = control;
                }
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }
        /// <summary>
        /// 입력 포커스가 컨트롤을 벗어나면 발생합니다.
        /// 라벨을 보이게하고 버튼을 숨긴다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async static void Event_Control_Leave(object sender, EventArgs e)
        {
            System.Windows.Forms.Control control;
            IDictionary dictionary;
            IAction action;
            Task<bool> task;

            try
            {
                if (sender is System.Windows.Forms.Control)//Windows Control 일 경우
                    control = (System.Windows.Forms.Control)sender;
                else
                    return;

                if (control.Tag is IDictionary)//ListOfValue Info가 있으면
                {
                    //선택된 항목을 가져온다
                    dictionary = (IDictionary)control.Tag;

                    if (dictionary.AutoComplete)
                    {
                        action = (IAction)dictionary;

                        if (dictionary.CurrentControl == null) dictionary.CurrentControl = control;

                        task = (Task<bool>)action.ControlAction(dictionary, "SearchAsync");
                        if (await task)
                        {
                            dictionary.CurrentControl = control;
                            SetResult(dictionary);
                            dictionary.CurrentControl = null;

                            foreach (System.Windows.Forms.Control _Con in control.Controls)
                            {
                                if (_Con is LabelControl)//입력된 Text가 없으면 워터마크 보이게
                                    _Con.Visible = control.Text.Length < 1;

                                if (_Con is SimpleButton)//버튼은 숨긴다
                                    _Con.Visible = false;
                            }
                        }
                    }
                    else
                    {
                        dictionary.CurrentControl = null;

                        foreach (System.Windows.Forms.Control _Con in control.Controls)
                        {
                            if (_Con is LabelControl)//입력된 Text가 없으면 워터마크 보이게
                                _Con.Visible = control.Text.Length < 1;

                            if (_Con is SimpleButton)//버튼은 숨긴다
                                _Con.Visible = false;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }
        /// <summary>
        /// 컨트롤에 포커스가 있을 때 키를 누르면 발생합니다.
        /// 엔터키가 입력되면 ListOfValue를 나타낸다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Event_Control_KeyPress(object sender, KeyPressEventArgs e)
        {
            System.Windows.Forms.Control control;
            XtraForm form;
            IDictionaryForm dictionaryForm;
            IDictionary dictionary;
            Keys keys;

            try
            {
                if (sender is System.Windows.Forms.Control)//Windows Control 일 경우
                    control = (System.Windows.Forms.Control)sender;
                else
                    return;

                if (control.Tag is IDictionary)//ListOfValue Info가 있으면
                {
                    dictionary = (IDictionary)control.Tag;

                    keys = (Keys)Enum.ToObject(typeof(Keys), Convert.ToInt32(e.KeyChar));
                    if (keys == Keys.Enter || keys == Keys.F2)//엔터키
                    {
                        if (dictionary.BeforeAction != null && !dictionary.BeforeAction.Invoke(dictionary.CurrentControl, (IBeforeEventArgs)dictionary))
                            return;

                        dictionary.CurrentControl = control;

                        dictionaryForm = dictionary.DictionaryForm;
                        dictionaryForm.Dictionary = dictionary;

                        form = (XtraForm)dictionaryForm;
                        form.Name = listDictionaryNames[dictionary.Code][ControlIndex(dictionary)];
                        form.Visible = true;
                    }
                }
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }
        /// <summary>
        /// 컨트롤에 Text가 변경되면 발생합니다.
        /// Dictionary에 검색할 Text를 지정합니다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Event_Control_TextChanged(object sender, EventArgs e)
        {
            System.Windows.Forms.Control control;
            IDictionary dictionary;

            try
            {
                if (sender is System.Windows.Forms.Control)//Windows Control 일 경우
                    control = (System.Windows.Forms.Control)sender;
                else
                    return;

                if (control.Tag is IDictionary)//ListOfValue Info가 있으면
                {
                    dictionary = (IDictionary)control.Tag;

                    foreach (System.Windows.Forms.Control _Con in control.Controls)
                        if (_Con is LabelControl && (dictionary.CurrentControl == null || !dictionary.CurrentControl.Equals(control)))//입력된 Text가 없으면 워터마크 보이게
                            _Con.Visible = control.Text.Length < 1;
                }
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }
        ///// <summary>
        ///// 컨트롤에 마우스가 집입하면 발생 합니다.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private static void Control_MouseEnter(object sender, EventArgs e)
        //{
        //    System.Windows.Forms.Control _Control;

        //    if (sender is System.Windows.Forms.Control)//Windows Control 일 경우
        //    {
        //        _Control = (System.Windows.Forms.Control)sender;

        //        if (_Control.Focused)
        //            return;

        //        foreach (System.Windows.Forms.Control _Con in _Control.Controls)
        //        {
        //            if (_Con is Button)
        //                if (_Con.Visible)
        //                    return;
        //        }

        //        Event_Control_Enter(sender, e);
        //    }
        //}
        ///// <summary>
        ///// 컨트롤에서 마우스가 벗어나면 발생 합니다.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private static void Control_MouseHover(object sender, EventArgs e)
        //{
        //    Control_MouseEnter(sender, e);
        //}
        ///// <summary>
        ///// 컨트롤에 마우스가 올라가면 발생 합니다.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private static void Control_MouseLeave(object sender, EventArgs e)
        //{
        //    System.Windows.Forms.Control _Control;

        //    if (sender is System.Windows.Forms.Control)//Windows Control 일 경우
        //    {
        //        _Control = (System.Windows.Forms.Control)sender;

        //        if (_Control.Focused)
        //            return;

        //        Event_Control_Leave(sender, e);
        //    }
        //}

        //private static void Label_MouseEnter(object sender, EventArgs e)
        //{
        //    System.Windows.Forms.Control _Control;

        //    if (sender is System.Windows.Forms.Control)//Windows Control 일 경우
        //    {
        //        _Control = (System.Windows.Forms.Control)sender;

        //        Control_MouseEnter(_Control.Parent, e);
        //    }
        //}
        ///// <summary>
        ///// 컨트롤에서 마우스가 벗어나면 발생 합니다.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private static void Label_MouseHover(object sender, EventArgs e)
        //{
        //    Control_MouseEnter(sender, e);
        //}
        ///// <summary>
        ///// 컨트롤에 마우스가 올라가면 발생 합니다.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private static void Label_MouseLeave(object sender, EventArgs e)
        //{
        //    System.Windows.Forms.Control _Control;

        //    if (sender is System.Windows.Forms.Control)//Windows Control 일 경우
        //    {
        //        _Control = (System.Windows.Forms.Control)sender;

        //        Event_Control_Leave(_Control.Parent, e);
        //    }
        //}

        /// <summary>
        /// 입력 포커스가 컨트롤을 벗어나면 발생합니다.
        /// 현재 Text 값으로 아이템을 찾음
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Event_ComboBox_Leave(object sender, EventArgs e)
        {
            ComboBoxEdit control;
            string tmp;

            try
            {
                if (sender is ComboBoxEdit)//ComboBox Control 일 경우
                    control = (ComboBoxEdit)sender;
                else
                    return;

                if (control.Tag is IDictionary)//ListOfValue Info가 있으면
                {
                    if (control.Properties.Items.Count < 1)
                        return;

                    if (control.Text.Length < 1)
                    {
                        if (control.SelectedIndex != -1)
                            control.SelectedIndex = -1;
                        else
                            Event_ComboBox_SelectedIndexChanged(sender, e);
                        return;
                    }

                    for (int i = 0; i < control.Properties.Items.Count; i++)
                        if (control.Text.Equals(control.Properties.Items[i].ToString()))
                        {
                            if (i != control.SelectedIndex)
                                control.SelectedIndex = i;
                            else
                                Event_ComboBox_SelectedIndexChanged(sender, e);
                        }

                    if (control.SelectedIndex != -1)
                    {
                        tmp = control.Text;
                        control.SelectedIndex = -1;
                        control.Text = tmp;
                    }
                    else
                        Event_ComboBox_SelectedIndexChanged(sender, e);
                }
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }
        /// <summary>
        /// 드롭다운 부분이 표시될 때 발생합니다.
        /// 등록된 아이템이 없으면 아이템을 가져옵니다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Event_ComboBox_DropDown(object sender, EventArgs e)
        {
            ComboBoxEdit control;
            IDictionary dictionary;
            IAfterEventArgs afterEventArgs;
            IAction action;

            try
            {
                if (sender is ComboBoxEdit)//Windows Control 일 경우
                    control = (ComboBoxEdit)sender;
                else
                    return;

                if (control.Properties.Items.Count < 1 && control.Tag is IDictionary)//아이템이 없고 ListOfValue Info가 있으면
                {
                    dictionary = (IDictionary)control.Tag;

                    dictionary.CurrentControl = control;

                    if (dictionary.BeforeAction != null && !dictionary.BeforeAction.Invoke(dictionary.CurrentControl, (IBeforeEventArgs)dictionary))
                        return;

                    action = (IAction)dictionary;

                    if ((bool)action.ControlAction(dictionary, "Search"))
                    {
                        control.Properties.Items.Clear();
                        afterEventArgs = dictionary;

                        foreach (DataRow _DataRow in afterEventArgs.DataTable.Rows)
                            control.Properties.Items.Add(new Item(_DataRow, control));

                        if (dictionary.AfterAction != null && !dictionary.AfterAction.Invoke(dictionary.CurrentControl, afterEventArgs))
                        {
                            afterEventArgs.DataTable = null;
                            afterEventArgs.DataRow = null;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }
        /// <summary>
        /// SelectedIndex 속성이 변경될 때 발생합니다.
        /// 택된 아이템을 가져옵니다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Event_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBoxEdit control;
            IDictionary dictionary;
            IAfterEventArgs afterEventArgs;

            try
            {
                if (sender is ComboBoxEdit)//Windows Control 일 경우
                    control = (ComboBoxEdit)sender;
                else
                    return;

                if (control.Tag is IDictionary)//ListOfValue Info가 있으면
                {
                    dictionary = (IDictionary)control.Tag;
                    afterEventArgs = dictionary;

                    if (control.SelectedIndex < 0)
                        afterEventArgs.DataRow = null;
                    else
                        afterEventArgs.DataRow = ((Item)control.SelectedItem).ItemRow;

                    dictionary.CurrentControl = control;

                    //선택된 아이템을 Description 컨트롤에 값을 할당합니다.
                    SetResult(dictionary);
                }
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }
        /// <summary>
        /// 컨트롤에 Text가 변경되면 발생합니다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Event_ComboBox_TextChanged(object sender, EventArgs e)
        {
            //Dim _Control As Windows.Forms.Control
            //Dim _Info As IDictionary
            //
            //If TypeOf sender Is ComboBox Then 'ComboBox Control 일 경우
            //    _Control = sender
            //
            //    If TypeOf _Control.Tag Is IDictionary Then 'ListOfValue Info가 있으면
            //        _Info = _Control.Tag
            //        '_Info.SearchText = _Control.Text '검색 문자열 반영
            //
            //        'If Not _Control.Focused Then
            //        '    Call Event_ComboBox_Leave(sender, e)
            //        'End If
            //    End If
            //End If
        }

        /// <summary>
        /// 라벨을 클릭시에 Lov 컨트롤로 포커스를 이동하여 입력할 수 있게 합니다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Event_Label_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Control control;

            try
            {
                if (sender is System.Windows.Forms.Control)//Windows Control 일 경우
                {
                    control = (System.Windows.Forms.Control)sender;
                    control = control.Parent;

                    if (control.Tag is IDictionary)
                        control.Focus();
                }
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }
        /// <summary>
        /// 버튼을 클릭하면 발생합니다.
        /// 폼을 나타냅니다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Event_Button_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Control control;

            try
            {
                if (sender is System.Windows.Forms.Control)//Windows Control 일 경우
                {
                    control = (System.Windows.Forms.Control)sender;

                    if (!(control is ButtonEdit))
                        control = control.Parent;

                    if (control.Tag is IDictionary)
                        Event_Control_KeyPress(control, new KeyPressEventArgs(Convert.ToChar(Keys.Enter)));
                }
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }
        #endregion

        #region "ETC"
        void IDictionary.Add(string code, BeforeAction beforeAction, AfterAction afterAction, params System.Windows.Forms.Control[] controls)
        {
            IDictionary dictionary;
            IAction action;
            System.Windows.Forms.Control currentControl;

            try
            {
                if (code == null || code.Equals(""))
                    throw new AtomusException("Code가 없습니다.");

                //if (controls == null)
                //    throw new AtomusException("Dictionary 기능을 추가할 컨트롤이 없습니다.");

                //if (controls.Length < 1)
                //    throw new AtomusException("Dictionary 기능을 추가할 컨트롤이 없습니다.");

                dictionary = this;

                dictionary.Code = code;
                dictionary.Controls = controls;
                dictionary.BeforeAction = beforeAction;
                dictionary.AfterAction = afterAction;
                dictionary.Where = "1 = 0";

                action = this;
                foreach (System.Windows.Forms.Control _TmpControl in controls)
                {
                    if (_TmpControl != null)
                    {
                        dictionary.CurrentControl = _TmpControl;
                        break;
                    }
                }

                if ((bool)action.ControlAction(this, "Search"))
                {
                    dictionary.CurrentControl = null;
                    dictionary.Where = "";

                    foreach (System.Windows.Forms.Control _TmpControl in controls)
                    {
                        if (_TmpControl == null)
                            continue;

                        if (_TmpControl is TextEdit || _TmpControl is ComboBoxEdit || _TmpControl is ButtonEdit)//Windows Control 일 경우
                        {
                            if (_TmpControl.Tag != null && !(_TmpControl.Tag is IDictionary))
                                throw new AtomusException(string.Format("{0}.Tag에 Dictionary가 아닌 값이 있습니다.", _TmpControl.Name));
                        }
                        else
                            throw new AtomusException("DevExpress.XtraEditors.TextEdit, DevExpress.XtraEditors.ComboBoxEdit 지원하는 Agent 입니다.");

                        //등록된 LOV가 있으면 삭제 한다.
                        ((IDictionary)this).Remove(_TmpControl);

                        _TmpControl.Tag = this;

                        if (!(_TmpControl is ComboBoxEdit) && (_TmpControl is TextEdit || _TmpControl is ButtonEdit))

                            if (dictionary.WaterMark)
                            {
                                if (listDictionaryNames.ContainsKey(dictionary.Code))
                                {
                                    currentControl = dictionary.CurrentControl;

                                    dictionary.CurrentControl = _TmpControl;

                                    try
                                    {
                                            SetWindowControl(_TmpControl, buttonText
                                            , listDictionaryNames[dictionary.Code][ControlIndex(this)]);
                                    }
                                    catch (Exception _Exception)
                                    {
                                        DiagnosticsTool.MyTrace(_Exception);
                                    }

                                    dictionary.CurrentControl = currentControl;
                                }
                            }
                            else
                                SetWindowControl(_TmpControl, buttonText, "");

                        if (_TmpControl is ComboBoxEdit)
                            SetWindowControl((ComboBoxEdit)_TmpControl);
                    }
                }
            }
            catch (Exception exception)
            {
                foreach (System.Windows.Forms.Control _TmpControl in controls)
                    ((IDictionary)this).Remove(_TmpControl);

                DiagnosticsTool.MyTrace(exception);
            }
        }
        //async void IDictionary.Add(string code, BeforeAction beforeAction, AfterAction afterAction, params System.Windows.Forms.Control[] controls)
        //{
        //    IDictionary dictionary;
        //    IAction action;
        //    System.Windows.Forms.Control currentControl;
        //    Task<bool> task;

        //    try
        //    {
        //        if (code == null || code.Equals(""))
        //            throw new AtomusException("Code가 없습니다.");

        //        if (controls == null)
        //            throw new AtomusException("Dictionary 기능을 추가할 컨트롤이 없습니다.");

        //        if (controls.Length < 1)
        //            throw new AtomusException("Dictionary 기능을 추가할 컨트롤이 없습니다.");

        //        dictionary = this;

        //        dictionary.Code = code;
        //        dictionary.Controls = controls;
        //        dictionary.BeforeAction = beforeAction;
        //        dictionary.AfterAction = afterAction;
        //        dictionary.Where = "1 = 0";

        //        action = this;
        //        foreach (System.Windows.Forms.Control _TmpControl in controls)
        //        {
        //            if (_TmpControl != null)
        //            {
        //                dictionary.CurrentControl = _TmpControl;
        //                break;
        //            }
        //        }
        //        task = (Task<bool>)action.ControlAction(this, "SearchAsync");

        //        if (await task)
        //        {
        //            dictionary.CurrentControl = null;
        //            dictionary.Where = "";

        //            foreach (System.Windows.Forms.Control _TmpControl in controls)
        //            {
        //                if (_TmpControl == null)
        //                    continue;

        //                if (_TmpControl is TextEdit || _TmpControl is ComboBoxEdit || _TmpControl is ButtonEdit)//Windows Control 일 경우
        //                {
        //                    if (_TmpControl.Tag != null && !(_TmpControl.Tag is IDictionary))
        //                        throw new AtomusException(string.Format("{0}.Tag에 Dictionary가 아닌 값이 있습니다.", _TmpControl.Name));
        //                }
        //                else
        //                    throw new AtomusException("DevExpress.XtraEditors.TextEdit, DevExpress.XtraEditors.ComboBoxEdit 지원하는 Agent 입니다.");

        //                //등록된 LOV가 있으면 삭제 한다.
        //                ((IDictionary)this).Remove(_TmpControl);

        //                _TmpControl.Tag = this;

        //                if (!(_TmpControl is ComboBoxEdit) && (_TmpControl is TextEdit || _TmpControl is ButtonEdit))

        //                    if (dictionary.WaterMark)
        //                    {
        //                        if (listDictionaryNames.ContainsKey(dictionary.Code))
        //                        {
        //                            currentControl = dictionary.CurrentControl;

        //                            dictionary.CurrentControl = _TmpControl;

        //                            try
        //                            {
        //                                SetWindowControl(_TmpControl, buttonText
        //                                , listDictionaryNames[dictionary.Code][ControlIndex(this)]);
        //                            }
        //                            catch (Exception _Exception)
        //                            {
        //                                DiagnosticsTool.MyTrace(_Exception);
        //                            }

        //                            dictionary.CurrentControl = currentControl;
        //                        }
        //                    }
        //                    else
        //                        SetWindowControl(_TmpControl, buttonText, "");

        //                if (_TmpControl is ComboBoxEdit)
        //                    SetWindowControl((ComboBoxEdit)_TmpControl);
        //            }
        //        }
        //    }
        //    catch (Exception exception)
        //    {
        //        foreach (System.Windows.Forms.Control _TmpControl in controls)
        //            ((IDictionary)this).Remove(_TmpControl);

        //        DiagnosticsTool.MyTrace(exception);
        //    }
        //}

        void Remove(System.Windows.Forms.Control control)
        {
            IDictionary dictionary;
            System.Windows.Forms.Control currentControl;

            if (control is System.Windows.Forms.Control)//Windows Control 일 경우
            {
                if (control.Tag == null || !(control.Tag is IDictionary))
                    return;

                dictionary = this;

                if (control is TextEdit || control is ButtonEdit)
                    if (dictionary.WaterMark)
                    {
                        if (listDictionaryNames.ContainsKey(dictionary.Code))
                        {
                            currentControl = dictionary.CurrentControl;

                            dictionary.CurrentControl = control;

                            RemoveTextBox(control, buttonText
                                , listDictionaryNames[dictionary.Code][ControlIndex(this)]);

                            dictionary.CurrentControl = currentControl;
                        }
                    }
                    else
                        RemoveTextBox(control, buttonText, "");

                if (control is ComboBoxEdit)
                    RemoveComboBox((ComboBoxEdit)control);

                control.Tag = null;
            }
        }
        void IDictionary.Remove(params System.Windows.Forms.Control[] controls)
        {
            foreach (System.Windows.Forms.Control _TmpControl in controls)
                this.Remove(_TmpControl);
        }

        /// <summary>
        /// TextEdit Dictionary 기능을 추가합니다.
        /// Dictionary 기능을 추가할 TextEdit를 지정합니다.
        /// </summary>
        /// <param name="textEdit"></param>
        /// <param name="buttonText"></param>
        private static void SetWindowControl(System.Windows.Forms.Control textEdit, string buttonText, string waterMark)
        {
            try
            {
                AddButton(textEdit, buttonText);//Lov 버튼 추가
                AddLabel(textEdit, waterMark);//Lov 라벨 추가(워터마크 표시)

                //이벤트 연결
                textEdit.Enter += Event_Control_Enter;//- Enter
                textEdit.Leave += Event_Control_Leave;//- 벗어날때
                textEdit.KeyPress += Event_Control_KeyPress;//- 키입력
                textEdit.TextChanged += Event_Control_TextChanged;//- Text가 변경 되면
                //_Control.MouseEnter += Control_MouseEnter;
                //_Control.MouseHover += Control_MouseHover;
                //_Control.MouseLeave += Control_MouseLeave;
            }
            catch (Exception exception)
            {
                if (textEdit != null)
                    textEdit.Tag = null;

                DiagnosticsTool.MyTrace(exception);
            }
        }

        /// <summary>
        /// ComboBoxEdit에 Dictionary 기능을 추가합니다.
        /// </summary>
        /// <param name="comboBoxEdit">Dictionary 기능을 추가할 ComboBoxEdit를 지정합니다.</param>
        private static void SetWindowControl(ComboBoxEdit comboBoxEdit)
        {
            try
            {
                comboBoxEdit.Properties.Items.Clear();
                comboBoxEdit.MaskBox.AutoCompleteMode = AutoCompleteMode.None;
                //이벤트 연결
                comboBoxEdit.Leave += Event_ComboBox_Leave;//- 벗어날때
                comboBoxEdit.ButtonClick += Event_ComboBox_DropDown;//- DropDown
                comboBoxEdit.SelectedIndexChanged += Event_ComboBox_SelectedIndexChanged;//- SelectedIndexChanged
                comboBoxEdit.TextChanged += Event_ComboBox_TextChanged;//- TextChanged

                Event_ComboBox_DropDown(comboBoxEdit, null);
            }
            catch (Exception exception)
            {
                if (comboBoxEdit != null)
                    comboBoxEdit.Tag = null;

                DiagnosticsTool.MyTrace(exception);
            }
        }

        /// <summary>
        /// TextEdit에 Dictionary 기능을 제거합니다.
        /// </summary>
        /// <param name="textEdit">Dictionary 기능을 제거할 TextEdit를 지정합니다.</param>
        /// <param name="buttonText"></param>
        private static void RemoveTextBox(System.Windows.Forms.Control textEdit, string buttonText, string waterMark)
        {
            IDictionary _Dictionary;

            try
            {
                _Dictionary = (IDictionary)textEdit.Tag;

                //이벤트 제거
                textEdit.TextChanged -= Event_Control_TextChanged;//- Text가 변경 되면
                textEdit.KeyPress -= Event_Control_KeyPress;//- 키입력
                textEdit.Leave -= Event_Control_Leave;//- 벗어날때
                textEdit.Enter -= Event_Control_Enter;//- Enter
                //_Control.MouseEnter -= Control_MouseEnter;
                //_Control.MouseHover -= Control_MouseHover;
                //_Control.MouseLeave -= Control_MouseLeave;

                RemoveLabel(textEdit, waterMark);//Lov 라벨 제거(워터마크 표시 제거)
                RemoveButton(textEdit, buttonText);//Lov 버튼 제거
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }
        /// <summary>
        /// comboBoxEdit에 Dictionary 기능을 제거합니다.
        /// </summary>
        /// <param name="comboBoxEdit">Dictionary 기능을 제거할 comboBoxEdit를 지정합니다.</param>
        private static void RemoveComboBox(ComboBoxEdit comboBoxEdit)
        {
            try
            {
                comboBoxEdit.TextChanged -= Event_ComboBox_TextChanged;//- TextChanged
                comboBoxEdit.SelectedIndexChanged -= Event_ComboBox_SelectedIndexChanged;//- SelectedIndexChanged
                comboBoxEdit.Popup -= Event_ComboBox_DropDown;//- DropDown
                comboBoxEdit.Leave -= Event_ComboBox_Leave;//- 벗어날때
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }

        /// <summary>
        /// Dictionary 버튼 생성하여 컨트롤에 추가합니다.
        /// </summary>
        /// <param name="control">Dictionary 버튼을 추가할 컨트롤을 지정합니다.</param>
        /// <param name="text"></param>
        private static void AddButton(System.Windows.Forms.Control control, string text)
        {
            SimpleButton button;
            ButtonEdit buttonEdit;
            Padding padding;

            try
            {
                if (control is ButtonEdit)
                {
                    buttonEdit = (ButtonEdit)control;
                    buttonEdit.ButtonClick += Event_Button_Click;
                }
                else
                {
                    //Lov 버튼 생성
                    button = new SimpleButton
                    {
                        Text = text,
                        //TextAlign = ContentAlignment.MiddleCenter,
                        Dock = DockStyle.Right
                    };

                    button.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                    button.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;

                    padding = new Padding(0);
                    button.Padding = padding;
                    button.Margin = padding;
                    button.Width = button.Height;
                    button.TabStop = false;
                    button.Cursor = Cursors.Hand;

                    button.Click += Event_Button_Click;

                    control.Controls.Add(button);
                    button.BringToFront();
                    button.Visible = false;
                }
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }
        /// <summary>
        /// 컨트롤에 라벨로 Dictionary 명을 표시합니다.
        /// </summary>
        /// <param name="control">Dictionary 컨트롤을 지정합니다.</param>
        /// <param name="text">표시할 문자를 지정합니다.</param>
        private static void AddLabel(System.Windows.Forms.Control control, string text)
        {
            LabelControl label;
            int byteRed;
            int byteGreen;
            int byteBlue;

            try
            {
                if (text.Equals(""))
                    return;

                label = new LabelControl
                {
                    Text = text,
                    AutoSize = true,
                    //Dock = DockStyle.Fill,
                    BackColor = control.BackColor
                };

                label.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
                label.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;

                if (control.BackColor.R < 128)
                    byteRed = control.BackColor.R + 64;
                else
                    byteRed = control.BackColor.R - 64;

                if (control.BackColor.G < 128)
                    byteGreen = control.BackColor.G + 64;
                else
                    byteGreen = control.BackColor.G - 64;

                if (control.BackColor.B < 128)
                    byteBlue = control.BackColor.B + 64;
                else
                    byteBlue = control.BackColor.B - 64;

                label.ForeColor = Color.FromArgb(byteRed, byteGreen, byteBlue);

                control.Controls.Add(label);

                label.Location = new Point(control.Margin.Left, control.Margin.Top);
                label.BringToFront();

                if (control.Text != "")
                    label.Visible = false;//컨트롤에 값이 있으면 안보이게

                label.Click += Event_Label_Click;
                //_Label.MouseEnter += Label_MouseEnter;
                //_Label.MouseHover += Label_MouseHover;
                //_Label.MouseLeave += Label_MouseLeave;
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }

        /// <summary>
        /// 컨트롤에 Dictionary 버튼을 제거합니다.
        /// </summary>
        /// <param name="control">Dictionary 버튼을 제거할 컨트롤을 지정합니다.</param>
        /// <param name="text"></param>
        private static void RemoveButton(System.Windows.Forms.Control control, string text)
        {
            SimpleButton button;

            button = null;

            try
            {
                foreach (System.Windows.Forms.Control _TmpControl in control.Controls)
                    if (_TmpControl is SimpleButton && _TmpControl.Text.Equals(text))
                    {
                        button = (SimpleButton)_TmpControl;
                        break;
                    }

                if (button != null)
                {
                    button.Click -= Event_Button_Click;
                    control.Controls.Remove(button);
                }
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }
        /// <summary>
        /// Dictionary 컨트롤에 라벨을 제거 합니다.
        /// </summary>
        /// <param name="control">Dictionary 컨트롤을 지정합니다.</param>
        /// <param name="text">제거할 라벨의 문자를 지정합니다.</param>
        private static void RemoveLabel(System.Windows.Forms.Control control, string text)
        {
            LabelControl label;

            label = null;

            try
            {
                if (text.Equals(""))
                    return;

                foreach (System.Windows.Forms.Control _TmpControl in control.Controls)
                    if (_TmpControl is LabelControl && _TmpControl.Text.Equals(text))
                    {
                        label = (LabelControl)_TmpControl;
                        break;
                    }

                if (label != null)
                {
                    label.Click -= Event_Label_Click;
                    //_Label.MouseEnter -= Label_MouseEnter;
                    //_Label.MouseHover -= Label_MouseHover;
                    //_Label.MouseLeave -= Label_MouseLeave;
                    control.Controls.Remove(label);
                }
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }

        /// <summary>
        /// 결과 값으로 컨트롤에 반영
        /// </summary>
        /// <param name="dictionary"></param>
        private static void SetResult(IDictionary dictionary)
        {
            IAfterEventArgs afterEventArgs;

            try
            {
                afterEventArgs = dictionary;

                //선택된 결과가 없고 자동완성이면 초기화
                if (afterEventArgs.DataRow == null && dictionary.AutoComplete)
                {
                    for (int i = 0; i < dictionary.Controls.Length; i++)
                        if (dictionary.Controls[i] is System.Windows.Forms.Control && !(dictionary.Controls[i] is ComboBoxEdit)) //윈도우 컨트롤이면
                            if ((dictionary.Controls[i]).Text.Equals(""))
                                dictionary.Controls[i].Text = "";//첫번쨰 컨트롤이 아닌 컨트롤을 초기화

                    return;
                }

                if (afterEventArgs.DataRow != null)
                {
                    for (int i = 0; i < dictionary.Controls.Length; i++)
                    {
                        if (dictionary.Controls[i] is System.Windows.Forms.Control) //윈도우 컨트롤이면
                            if (i < afterEventArgs.DataRow.ItemArray.Length) //Lov 결과보다 컨트롤의 수가 적다면
                            {
                                if (afterEventArgs.DataRow[i] != DBNull.Value)//결과 값이 있다면
                                    dictionary.Controls[i].Text = afterEventArgs.DataRow[i].ToString();

                                else//결과 값이 없다면
                                    dictionary.Controls[i].Text = "";
                            }
                    }

                    if (dictionary.AfterAction != null && !dictionary.AfterAction.Invoke(dictionary.CurrentControl, afterEventArgs))
                    {
                        afterEventArgs.DataTable = null;
                        afterEventArgs.DataRow = null;
                    }
                }
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }

        private static int ControlIndex(IDictionary dictionary)
        {
            int controlIndex;

            controlIndex = 0;
            foreach (System.Windows.Forms.Control _Cont in dictionary.Controls)
            {
                if (_Cont == null)
                {
                    controlIndex += 1;
                    continue;
                }

                if (_Cont.Equals(dictionary.CurrentControl))
                    break;

                controlIndex += 1;
            }

            return controlIndex;
        }
        #endregion
    }
}
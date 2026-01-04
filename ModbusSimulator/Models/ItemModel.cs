using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ModbusSimulator.Models
{
    public class ItemModel : BindableBase
    {
        public ItemModel()
        {
        
        }

        public ItemModel(int index, string value)
        {
            Index = index;
            Value = value;
        }


        private int index;
        public int Index
        {
            get { return index; }
            set { SetProperty(ref index, value); }
        }

        private string value;
        public string Value
        {
            get { return value; }
            set { SetProperty(ref this.value, value); }
        }


    }
}

using System;
using EQuant;
using EQuant.STG;

// 向程序集中声明策略
[assembly: SeniorStrategy(typeof(EQuant.ArbitrageGridStrategy.ArbitrageGridTradeDefine))]

namespace EQuant.ArbitrageGridStrategy
{
    /// <summary>
    /// 示例策略1的定义
    /// </summary>
    class ArbitrageGridTradeDefine : SeniorFixedStrategyDefine
    {
        /// <summary>
        /// 获取策略名称
        /// </summary>
        public override string Name
        {
            get { return "ArbitrageGridHedgeStrategy"; }
        }

        /// <summary>
        /// 获取策略标题
        /// </summary>
        public override string Title
        {
            get { return "网格套利交易策略"; }
        }


        /// <summary>
        /// 获取有关策略的说明
        /// </summary>
        public override string Description
        {
            get { return "网格套利交易报单策略"; }
        }

        /// <summary>
        /// 获取策略开发商
        /// </summary>
        public override string Company
        {
            get { return "永安期货股份有限公司"; }
        }

        /// <summary>
        /// 获取合约数量
        /// </summary>
        public override int ContractCount
        {
            get { return 2; }
        }

        /// <summary>
        /// 构造一个示例策略的新实例
        /// </summary>
        public ArbitrageGridTradeDefine()
            : base(new Guid("{0C91885B-BA6C-43B8-AB34-7A375E42193E}"))
        {
            base.ForeCount = 10;
            //设置策略分组
            base.Subgroup = "算法交易工具（网格类）";


            //设置可接受周期
            base.Periods.Add(MinuteType.Tick);
            //base.Periods.Add(MinuteType.BarMin3);
            //base.Periods.Add(MinuteType.BarMin5);
            //base.Periods.Add(MinuteType.BarMin15);
            //base.Periods.Add(MinuteType.BarDay);

            //添加参数定义
            IntParameterDefine pInt;
            EnumParameterDefine pEnum;
            DoubleParameterDefine pDouble;
           // BoolParameterDefine pBool;

            //pInt = new IntParameterDefine("P1");
            //pInt.Title = "总目标手数";
            //pInt.MaxValue = 100000;
            //pInt.MinValue = 0;
            //base.ParameterDefines.Add(pInt);

            //pInt = new IntParameterDefine("P2");
            //pInt.Title = "已实现目标手数";
            //pInt.MaxValue = 100000;
            //pInt.MinValue = 0;
            //base.ParameterDefines.Add(pInt);

            pEnum = new EnumParameterDefine("P1");
            pEnum.Title = "价差计算方式";
            pEnum.ValueNames = new string[] { "第一腿减第二腿", "第一腿除以第二腿" };
            base.ParameterDefines.Add(pEnum);

            pInt = new IntParameterDefine("P2");
            pInt.Title = "第一腿乘数";
            pInt.MaxValue = 500;
            pInt.MinValue = 1;
            base.ParameterDefines.Add(pInt);

            pInt = new IntParameterDefine("P3");
            pInt.Title = "第一腿补充值";
            pInt.MaxValue = 500;
            pInt.MinValue = 1;
            base.ParameterDefines.Add(pInt);

            pInt = new IntParameterDefine("P4");
            pInt.Title = "第二腿乘数";
            pInt.MaxValue = 500;
            pInt.MinValue = 1;
            base.ParameterDefines.Add(pInt);

            pInt = new IntParameterDefine("P5");
            pInt.Title = "第二腿补充值";
            pInt.MaxValue = 500;
            pInt.MinValue = 1;
            base.ParameterDefines.Add(pInt);

            pEnum = new EnumParameterDefine("P6");
            pEnum.Title = "买卖方向";
            pEnum.ValueNames = new string[] { "买", "卖" };
            base.ParameterDefines.Add(pEnum);

            pEnum = new EnumParameterDefine("P7");
            pEnum.Title = "开平";
            pEnum.ValueNames = new string[] { "开仓", "平仓" };
            base.ParameterDefines.Add(pEnum);

            pDouble = new DoubleParameterDefine("P8");
            pDouble.Title = "上边界";
            pDouble.MaxValue = 100000;
            pDouble.MinValue = -100000;
            pDouble.DecimalPlaces = 3;
            base.ParameterDefines.Add(pDouble);

            pDouble = new DoubleParameterDefine("P9");
            pDouble.Title = "下边界";
            pDouble.MaxValue = 100000;
            pDouble.DecimalPlaces = 3;
            pDouble.MinValue = -100000;
            base.ParameterDefines.Add(pDouble);

            pDouble = new DoubleParameterDefine("P10");
            pDouble.Title = "网格正向间隔";
            pDouble.MaxValue = 1000;
            pDouble.DecimalPlaces = 3;
            pDouble.MinValue = 0;
            base.ParameterDefines.Add(pDouble);

            pDouble = new DoubleParameterDefine("P11");
            pDouble.Title = "网格反向间隔";
            pDouble.MaxValue = 1000;
            pDouble.DecimalPlaces = 3;
            pDouble.MinValue = 0;
            base.ParameterDefines.Add(pDouble);

            pInt = new IntParameterDefine("P12");
            pInt.Title = "预先挂单间隔（最小变动价位）";
            pInt.MinValue = 1;
            pInt.MaxValue = 10;
            pInt.DefaultValue = 3;
            base.ParameterDefines.Add(pInt);

            pInt = new IntParameterDefine("P13");
            pInt.Title = "每次第一腿下单手数";
            pInt.MaxValue = 500;
            pInt.MinValue = 1;
            base.ParameterDefines.Add(pInt);

            pInt = new IntParameterDefine("P14");
            pInt.Title = "每次第二腿下单手数";
            pInt.MaxValue = 500;
            pInt.MinValue = 1;
            base.ParameterDefines.Add(pInt);

            pInt = new IntParameterDefine("P15");
            pInt.Title = "第一腿超价";
            pInt.MinValue = 1;
            pInt.MaxValue = 100;
            pInt.DefaultValue = 1;
            base.ParameterDefines.Add(pInt);

            pInt = new IntParameterDefine("P16");
            pInt.Title = "第一腿撤单等待（秒）";
            pInt.MinValue = 1;
            pInt.MaxValue = 100;
            pInt.DefaultValue = 1;
            base.ParameterDefines.Add(pInt);
        }

        //<summary>
        //创建策略执行器
        //</summary>
        /// <remarks>返回此策略定义的执行逻辑</remarks>
        protected override SeniorStrategyExecuter CreateExecuter()
        {
            return new ArbitrageGridExecuter();
        }

        /// <summary>
        /// 动态调整参数
        /// </summary>
        /// <param name="e"></param>
        protected override void DynamicAdapt(ValueAdaptArgs e)
        {
            //e.Values[1].Enabled = false;
            //if (e.Values[10].BoolValue == true)
            //    e.Values[11].Enabled = true;
            //else
            //    e.Values[11].Enabled = false;
            //if (e.Values[2].IntValue == 0)
            //{
            //    e.Values[4].Enabled = true;
            //    e.Values[5].Enabled = false;
            //    if (e.Values[8].IntValue != 0)
            //        e.Values[5].DoubleValue = e.Values[4].DoubleValue - e.Values[6].DoubleValue * ((int)e.Values[0].IntValue / e.Values[8].IntValue);
            //}
            //else
            //{
            //    e.Values[4].Enabled = false;
            //    e.Values[5].Enabled = true;
            //    if (e.Values[8].IntValue != 0)
            //        e.Values[4].DoubleValue = e.Values[5].DoubleValue + e.Values[6].DoubleValue * ((int)e.Values[0].IntValue / e.Values[8].IntValue);
            //}
            //if (e.Values[13].BoolValue == true)
            //{
            //    e.Values[14].Enabled = false;
            //}
            //else
            //{
            //    e.Values[14].Enabled = true;
            //}

            //int temp = (e.Values[4].IntValue - e.Values[5].IntValue);
            //int temp1;
            //if (e.Values[6].IntValue != 0)
            //{
            //	temp1 = (temp / e.Values[6].IntValue);
            //	if (temp1 != 0)
            //		e.Values[8].IntValue = e.Values[0].IntValue / temp1;

            //}
            //e.Values[8].Enabled = false;
        }

        /// <summary>
        /// 参数合法性检查
        /// </summary>
        /// <param name="e">数据信息</param>
        public override void CheckValue(ValueCheckArgs e)
        {
            //e.Succeed = true;
            ////买入
            //if (e.Values[2].IntValue == 0)
            //{
            //    if ((decimal)e.Values[4].DoubleValue % (decimal)e.Products[0].PriceTick != 0)
            //    {
            //        e.Succeed = false;
            //        e.Message = "设置的网格上边界价格不正确!";
            //    }
            //    else if ((bool)e.Values[10].BoolValue && (decimal)e.Values[11].DoubleValue > (decimal)e.Values[5].DoubleValue)
            //    {
            //        e.Succeed = false;
            //        e.Message = "多头止损价格不能大于网格下边界!";
            //    }
            //}
            ////卖出
            //else
            //{
            //    if ((decimal)e.Values[5].DoubleValue % (decimal)e.Products[0].PriceTick != 0)
            //    {
            //        e.Succeed = false;
            //        e.Message = "设置的网格下边界价格不正确!";
            //    }
            //    else if ((bool)e.Values[10].BoolValue && (decimal)e.Values[11].DoubleValue < (decimal)e.Values[4].DoubleValue)
            //    {
            //        e.Succeed = false;
            //        e.Message = "空头止损价格不能小于网格上边界!";
            //    }
            //}
            //if ((decimal)e.Values[6].DoubleValue % (decimal)e.Products[0].PriceTick != 0)
            //{
            //    e.Succeed = false;
            //    e.Message = "设置的网格间隔不正确!";
            //}
            //else if ((decimal)e.Values[7].DoubleValue % (decimal)e.Products[0].PriceTick != 0)
            //{
            //    e.Succeed = false;
            //    e.Message = "设置的网格反向间隔不正确!";
            //}
            //else if ((decimal)e.Values[7].DoubleValue <= e.Values[12].IntValue * (decimal)e.Products[0].PriceTick)
            //{
            //    e.Succeed = false;
            //    e.Message = "反向间隔设置失败，必须大于预先挂单间隔!";
            //}
            //else if ((bool)e.Values[10].BoolValue && (decimal)e.Values[11].DoubleValue % (decimal)e.Products[0].PriceTick != 0)
            //{
            //    e.Succeed = false;
            //    e.Message = "设置的网格止损价格不正确!";
            //}

        }

        public override void CheckRange(RangeCheckArgs e)
        {
            //TODO:检查逻辑
            //若检查失败
            //e.Succeed = false;
            //if(e.Values["P2"].IntValue > 0)
            //e.SkipCheck(2);

            //若检查成功
            e.Succeed = true;
        }
    }//class	
}

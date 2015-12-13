using EQuant;
using EQuant.STG;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;


namespace EQuant.ArbitrageGridStrategy
{
    public class ArbitrageGridExecuter : SeniorStrategyExecuter
    {
        #region xml配置类
        class save : IXmlSerializable
        {
            public DirectionType SaveDir;//买卖方向
            public OffsetFlagType SaveOff;//开平
            public double SaveUpLine;//上边界
            public double SaveDownLine;//下边界
            public double SaveGridData;
            //上一次所在的网格
            public int SaveIndex;
            public DateTime nowDate;//日期
            public List<EachGridInfo> GridList;//网格列表

            public save()
            {
                this.GridList = new List<EachGridInfo>();
            }

            //获取XML架构
            System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema()
            {
                return null;
            }

            //读取XML
            void IXmlSerializable.ReadXml(XmlReader reader)
            {
                reader.ReadToDescendant("Root");
                Enum.TryParse<DirectionType>(reader["SaveDir"], out SaveDir);
                Enum.TryParse<OffsetFlagType>(reader["SaveOff"], out SaveOff);
                double.TryParse(reader["SaveUpLine"], out SaveUpLine);
                double.TryParse(reader["SaveDownLine"], out SaveDownLine);
                double.TryParse(reader["SaveGridData"], out SaveGridData);
                int.TryParse(reader["SaveIndex"], out SaveIndex);
                DateTime.TryParse(reader["nowDate"], out nowDate);

                if (!reader.IsEmptyElement)
                {
                    reader.Read();
                    while (reader.IsStartElement())
                        switch (reader.Name)
                        {
                            case "List":
                                this.ReadList(reader);
                                continue;
                            default:
                                reader.Skip();
                                continue;
                        }
                }
                reader.Read();
            }

            void ReadList(XmlReader reader)
            {
                if (!reader.IsEmptyElement)
                {
                    reader.Read();
                    while (reader.IsStartElement())
                        switch (reader.Name)
                        {
                            case "item":
                                EachGridInfo item = new EachGridInfo();
                                ((IXmlSerializable)item).ReadXml(reader);
                                this.GridList.Add(item);
                                continue;
                            default:
                                reader.Skip();
                                continue;
                        }
                }
                reader.Read();
            }

            //写入XML
            void IXmlSerializable.WriteXml(XmlWriter writer)
            {
                writer.WriteStartElement("Root");
                writer.WriteAttributeString("SaveDir", this.SaveDir.ToString());
                writer.WriteAttributeString("SaveOff", this.SaveOff.ToString());
                writer.WriteAttributeString("SaveUpLine", this.SaveUpLine.ToString());
                writer.WriteAttributeString("SaveDownLine", this.SaveDownLine.ToString());
                writer.WriteAttributeString("SaveGridForwardInterval", this.SaveGridData.ToString());
                writer.WriteAttributeString("SaveIndex", this.SaveIndex.ToString());
                writer.WriteAttributeString("nowDate", this.nowDate.ToShortDateString());

                writer.WriteStartElement("List");
                foreach (EachGridInfo status in GridList)
                {
                    writer.WriteStartElement("item");
                    ((IXmlSerializable)status).WriteXml(writer);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }
        #endregion
        class ForwardOrderType
        {
            public IStrategyOrder firstLegOrder;
            public IStrategyOrder secondLegOrder;
        }
        class BackwardOrderType
        {
            public IStrategyOrder firstLegOrder;
            public IStrategyOrder secondLegOrder;
        }
        class EachGridInfo : IXmlSerializable
        {
            //该网格是否已经挂有止盈单子
            public bool Isopposite;
            //该网格今日成交单的数量
            public int TodayVolume;
            //该网格今日第一腿成交单的数量
            public int firstLegVolume;
            //该网格今日第二腿成交单的数量
            public int secondLegVolume;
            public object tag;
            //该网格对应的价格
            public double Price;
            //该网格上成交过的总数量
            public int totalVolume;
            //是否已经挂单
            public bool IsSetOrder;
            //记录正向今仓挂单
            public ForwardOrderType ForwardOrder;
            //记录止盈单今仓挂单
            public BackwardOrderType OppositeOrder;
            //记录正向昨仓挂单
            public IStrategyOrder YdForwardOrder;
            //记录止盈单昨仓挂单
            public IStrategyOrder YdOppositeOrder;


            //获取XML架构
            System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema()
            {
                return null;
            }

            //读取XML
            void IXmlSerializable.ReadXml(XmlReader reader)
            {
                bool.TryParse(reader["Isopposite"], out Isopposite);
                int.TryParse(reader["TodayVolume"], out TodayVolume);
                double.TryParse(reader["Price"], out Price);
                bool.TryParse(reader["IsSetOrder"], out IsSetOrder);
                int.TryParse(reader["totalVolume"], out totalVolume);
                reader.Read();
            }

            //写入XML
            void IXmlSerializable.WriteXml(XmlWriter writer)
            {
                writer.WriteAttributeString("Isopposite", this.Isopposite.ToString());
                writer.WriteAttributeString("TodayVolume", this.TodayVolume.ToString());
                writer.WriteAttributeString("Price", this.Price.ToString());
                writer.WriteAttributeString("IsSetOrder", this.IsSetOrder.ToString());
                writer.WriteAttributeString("totalVolume", this.totalVolume.ToString());
            }
        }
        StrategyConfig config;
        ITick firstLegTick;
        ITick secondLegTick;
        private int firstLegTime;
        private int firstLegAddition;
        private int secondLegTime;
        private int secondLegAddition;
        private DirectionType direction;
        private OffsetFlagType statusFlag;
        private double upLine;
        private double downLine;
        private double GridForwardInterval;
        private double GridBackwardInterval;
        private int AdvanceOrder;
        private int EachFirstLegVol;
        private int EachSecondLegVol;
        private int firstPassPrice;
        private bool IsSHFE;
        private int firstDelayCancel;
        private int indexRecord;
        private bool isRecord;
        private IStrategyOrder tempOrder;
        //正向提前挂单范围
        private double ForData;
        //反向提前挂单范围
        private double OppData;
        private bool isOpposite;
        private double bidSpread;
        private double askSpread;
        private save saveData;
        private IStrategyOrder order1;//第一腿报单 
        private IStrategyOrder order2;//第二腿报单
        private double proportion;
        protected override void OnInit()
        {
            this.Positions[0].OnTick += ArbitrageGridExecuter_OnTick;
            this.Positions[1].OnTick += ArbitrageGridExecuter_OnTick;
            this.Positions[0].OnTrade += ArbitrageGridExecuter_OnTrade;
            this.Positions[1].OnTrade += ArbitrageGridExecuter_OnTrade;
            this.Positions[0].OnOrder += PriorityHedgeTradeExecuter_OnOrder1;
            this.Positions[1].OnOrder += PriorityHedgeTradeExecuter_OnOrder2;
            this.Positions[0].OnOrderFailed += PriorityHedgeTradeExecuter_OnOrderFailed;
            this.Positions[1].OnOrderFailed += PriorityHedgeTradeExecuter_OnOrderFailed;
            this.ForData = 3 * this.Positions[0].Contract.Product.PriceTick;
            this.OppData = 3 * this.Positions[0].Contract.Product.PriceTick;

            this.saveData = new save();
            try
            {
                Utility.ReadFromXML(ConfigName + ".xml", this.saveData);
            }
            catch (Exception)
            {

                this.Log.WriteLine(MsgType.Warning, "重新载入配置表！");
            }
            //判断是否上期所
            if (this.Positions[0].Contract.Product.ExchangeID == "SHFE")
            {
                this.IsSHFE = true;
            }
            //判断日期，第二天重新初始化
            if (saveData.nowDate != DateTime.Today)
            {
                for (int i = 0; i < saveData.GridList.Count; i++)
                {
                    saveData.GridList[i].IsSetOrder = false;
                    saveData.GridList[i].Isopposite = false;
                }
                saveData.nowDate = DateTime.Today;
            }
            //写xml       
            try
            {
                Utility.WriteToXML(ConfigName + ".xml", this.saveData);
            }
            catch (System.IO.FileNotFoundException)
            {
                this.Log.WriteLine(MsgType.Warning, "第一次初始化配置！");
            }
            catch (Exception)
            {
                this.Log.WriteLine(MsgType.Error, "写入配置表错误！");
            }
        }
        void PriorityHedgeTradeExecuter_OnOrder1(object sender, OrderEventArgs e)
        {
            Log.WriteLine("第一腿" + e.StrategyOrder.OrderStatus + e.RawOrder.OrderSysID);
            if (order1 == null)
            {
                Log.WriteLine(MsgType.Warning, "第一腿订单不存在!");
                return;
            }
            if (e.StrategyOrder.OrderStatus == OrderStatusType.全部成交 || e.StrategyOrder.OrderStatus == OrderStatusType.已撤单)
            {
                ((IStrategyTimer)order1.Tag).Dispose();
                order1 = null;
            }
            if (e.StrategyOrder.OrderStatus == OrderStatusType.尚未成交 || e.StrategyOrder.OrderStatus == OrderStatusType.部分成交)
            {
                isRecord = true;
            }
        }
        void PriorityHedgeTradeExecuter_OnOrder2(object sender, OrderEventArgs e)
        {
            Log.WriteLine("第二腿" + e.StrategyOrder.OrderStatus + e.RawOrder.OrderSysID);
            if (order2 == null)
            {
                Log.WriteLine(MsgType.Warning, "第二腿订单不存在!");
                return;
            }
            //判断第二腿状态
            //报单为开仓单时
            if (e.StrategyOrder.OrderStatus == OrderStatusType.全部成交 || e.StrategyOrder.OrderStatus == OrderStatusType.已撤单)
            {
                ((IStrategyTimer)order2.Tag).Dispose();
                order2 = null;
            }
        }
        void PriorityHedgeTradeExecuter_OnOrderFailed(object sender, OrderFailedEventArgs e)
        {
            Log.WriteLine(e.StrategyOrder.Contract.InstrumentID + e.RawOrder.OrderSysID + "报单失败！");
            if (e.StrategyOrder.Contract.InstrumentID == this.Positions[0].Contract.InstrumentID)
                order1 = null;
            else
                order2 = null;
        }
        void ArbitrageGridExecuter_OnTrade(object sender, TradeEventArgs e)
        {
            //第一腿成交回报
            if (e.StrategyTrade.Order.Contract.InstrumentID == this.Positions[0].Contract.InstrumentID)
            {
                foreach (EachGridInfo Grid in saveData.GridList)
                {
                    if (Grid.tag == e.StrategyTrade.Order.Tag)
                    {
                        Grid.firstLegVolume += e.StrategyTrade.Volume;
                    }
                }
            }
            //第二退成交回报
            else
            {
                foreach (EachGridInfo Grid in saveData.GridList)
                {
                    if (Grid.tag == e.StrategyTrade.Order.Tag)
                    {
                        Grid.secondLegVolume += e.StrategyTrade.Volume;
                    }
                }
            }
            Utility.WriteToXML(this.WorkDirectory + "\\" + ConfigName + ".xml", this.saveData);
        }


        //计算价差
        void ArbitrageGridExecuter_OnTick(object sender, PositionTickEventArgs e)
        {
            if (isRecord)
                indexRecord = e.TickIndex-1;
            if (order1 != null)
            {
                if ((e.TickIndex - indexRecord) / 2 == firstDelayCancel)
                {
                    tempOrder = order1;
                    if (tempOrder.Direction == DirectionType.买)
                    {
                        order1 = this.Positions[0].InputOrder(firstLegTick.AskPrice + firstPassPrice, EachFirstLegVol, tempOrder.Direction, tempOrder.OffsetFlag, tempOrder.Tag);
                        indexRecord = -1;
                    }
                    else
                    {
                        order1 = this.Positions[0].InputOrder(firstLegTick.BidPrice - firstPassPrice, EachFirstLegVol, tempOrder.Direction, tempOrder.OffsetFlag, tempOrder.Tag);
                        indexRecord = -1;
                    }
                }
            }
            if (e.Position == this.Positions[0])
            {
                firstLegTick = e.Tick;
            }
            else
            {
                secondLegTick = e.Tick;
            }
            if (firstLegTick == null || secondLegTick == null)
                return;
            bidSpread = (firstLegTime*firstLegTick.AskPrice+firstLegAddition) - (secondLegTime*secondLegTick.BidPrice+secondLegAddition);
            askSpread = (firstLegTime * firstLegTick.BidPrice + firstLegAddition) - (secondLegTime * secondLegTick.AskPrice + secondLegAddition);
            proportion = EachFirstLegVol / EachSecondLegVol;
            GridExecution(e);
        }

        //网格判断
        void GridExecution(PositionTickEventArgs e)
        {
            int position = Calculate(Math.Min(askSpread, bidSpread));
            try
            {
                if (statusFlag == OffsetFlagType.开仓)
                {
                    if (direction == DirectionType.卖)
                    {
                        foreach (EachGridInfo grid in saveData.GridList)
                        {
                            //当前价格接近或大于正向开仓间隔
                            double priceDiff1 = grid.Price - askSpread;
                            double priceDiff2 = bidSpread - (grid.Price - GridBackwardInterval);
                            if ( priceDiff1 < ForData && priceDiff1 > 0)
                            {
                                //未挂过正向单
                                if (!grid.IsSetOrder)
                                {
                                    int Temp = EachSecondLegVol - grid.secondLegVolume;
                                    if (Temp > 0)
                                    {
                                        PriorityOrderLogic(Temp, DirectionType.卖, OffsetFlagType.开仓, grid.Price);
                                        grid.IsSetOrder = true;
                                    }

                                }
                            }
                            //当前价格接近或大于反向止盈间隔
                            if ( priceDiff2 < OppData && priceDiff2 > 0)
                            {
                                int temp = grid.TodayVolume;
                                if (temp != 0)
                                {
                                    if (order2 != null)
                                        order2.Tag = Timer.Call(OnTimerCancel, order2);
                                    //未挂过止盈单
                                    if (!grid.Isopposite)
                                    {
                                        grid.Isopposite = true;
                                        PriorityOrderLogic(temp, DirectionType.买, OffsetFlagType.平仓, grid.Price);
                                    }
                                }

                            }
                        }
                    }
                    else if (direction == DirectionType.买)
                    {
                        //扫描网格
                        foreach (EachGridInfo Grid in saveData.GridList)
                        {
                            double priceDiff1 = bidSpread - Grid.Price;
                            double priceDiff2 = (Grid.Price + GridBackwardInterval) - bidSpread;
                            //当前价格接近或大于正向开仓间隔
                            if (priceDiff1 < ForData && priceDiff1 > 0)
                            {
                                //未挂过正向单
                                if (!Grid.IsSetOrder)
                                {
                                    int Temp = EachSecondLegVol - Grid.secondLegVolume;
                                    if (Temp > 0)
                                    {
                                        PriorityOrderLogic(Temp, DirectionType.买, OffsetFlagType.开仓, Grid.Price);
                                        Grid.IsSetOrder = true;
                                    }
                                }
                            }
                            //当前价格接近或大于反向止盈间隔
                            if (priceDiff2 < OppData && priceDiff2 > 0)
                            {
                                int temp = Grid.TodayVolume;
                                if (temp != 0)
                                {
                                    if (order2 != null)
                                        order2.Tag = Timer.Call(OnTimerCancel, order2);
                                    if (!Grid.Isopposite)
                                    {
                                        Grid.Isopposite = true;
                                        PriorityOrderLogic(temp, DirectionType.卖, OffsetFlagType.平仓, Grid.Price);
                                    }
                                }
                            }
                        }
                    }
                }
                //平仓的情况
                else
                {
                    #region 卖平的情况
                    if (direction == DirectionType.卖)
                    {
                        foreach (EachGridInfo Grid in saveData.GridList)
                        {
                            double priceDiff1 = Grid.Price - askSpread;
                            double priceDiff2 = askSpread - (Grid.Price - GridBackwardInterval);
                            //当前价格接近或大于正向开仓间隔
                            if ( priceDiff1 < ForData && priceDiff1 > 0)
                            {
                                    if (!Grid.IsSetOrder)
                                    {
                                        int Temp = EachSecondLegVol - Grid.secondLegVolume;
                                        if (Temp > 0)
                                        {
                                            PriorityOrderLogic(Temp, DirectionType.卖, OffsetFlagType.平仓, Grid.Price);
                                            Grid.IsSetOrder = true;
                                        }

                                    }
                            }
                            if (priceDiff2 < OppData && priceDiff2 >0)
                            {
                                if (order2 != null)
                                    order2.Tag = Timer.Call(OnTimerCancel, order2);
                                //未挂过止盈单
                                if (!Grid.Isopposite)
                                {
                                    int temp = Grid.TodayVolume;
                                    if (temp != 0)
                                    {
                                        if (order2 != null)
                                            order2.Tag = Timer.Call(OnTimerCancel, order2);
                                        if (!Grid.Isopposite)
                                        {
                                            Grid.Isopposite = true;
                                            PriorityOrderLogic(temp, DirectionType.买, OffsetFlagType.开仓, Grid.Price);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                    #region 买平的情况
                    else if (direction == DirectionType.买)
                    {
                        //扫描网格
                        foreach (EachGridInfo Grid in saveData.GridList)
                        {
                            double priceDiff1 = bidSpread - Grid.Price;
                            double priceDiff2 = (Grid.Price + GridBackwardInterval) - bidSpread;
                            //当前价格接近或大于正向开仓间隔
                            if (priceDiff1 < ForData && priceDiff1 >0)
                            {
                                    if (!Grid.IsSetOrder)
                                    {
                                        int Temp = EachSecondLegVol - Grid.secondLegVolume;
                                        if (Temp > 0)
                                        {
                                            PriorityOrderLogic(Temp, DirectionType.买, OffsetFlagType.平仓, Grid.Price);
                                            Grid.IsSetOrder = true;
                                        }
                                    }
                            }
                            //当前价格接近或大于反向止盈间隔
                            if ( priceDiff2 < OppData && priceDiff2 > 0)
                            {
                                if (order2 != null)
                                    order2.Tag = Timer.Call(OnTimerCancel, order2);
                                if (!Grid.Isopposite)
                                {
                                    int temp = Grid.TodayVolume;
                                    if (temp != 0)
                                    {
                                        if (order2 != null)
                                            order2.Tag = Timer.Call(OnTimerCancel, order2);
                                        if (!Grid.Isopposite)
                                        {
                                            Grid.Isopposite = true;
                                            PriorityOrderLogic(temp, DirectionType.卖, OffsetFlagType.开仓, Grid.Price);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                }
                //保存当前的网格索引号
            }
            catch (InvalidOrderException exp)
            {
                if (exp.Reason == InvalidOrderReason.InvestorLimit)
                {
                    this.Log.WriteLine(MsgType.Error, "该账户被禁止报单，请勾选账户属性中允许报单");
                    throw;
                }
                if (exp.Reason == InvalidOrderReason.PositionLimit)
                {
                    this.Log.WriteLine(MsgType.Error, "该合约被禁止报单，请设置账户下的该合约报单标志位绿色");
                    throw;
                }
                if (exp.Reason == InvalidOrderReason.SelfTrade)
                {
                    this.Log.WriteLine(MsgType.Warning, "该报单存在自成交风险，已被拒绝！");
                }
                if (exp.Reason == InvalidOrderReason.VolumeLimit)
                {
                    this.Log.WriteLine(MsgType.Warning, "该报单超限仓，已被拒绝！");
                }
            }
            catch (OperationCanceledException)
            {
                this.Log.WriteLine(MsgType.Warning, "非正数的报单！");
            }
            saveData.SaveIndex = position;
        }

        //IStrategyOrder InputOrder(int volume, DirectionType dir1, OffsetFlagType off1, double tag)
        //{
        //    //若发现下单手数为负，反向报单，并通知平台，将Tag置为1
        //    if (volume < 0)
        //    {
        //        volume = Math.Abs(volume);
        //        if (this.direction == dir1)
        //        {
        //            if (this.direction == DirectionType.买)
        //            {
        //                tag += GridBackwardInterval;
        //            }
        //            else
        //            {
        //                tag -= GridBackwardInterval;
        //            }
        //        }
        //        else
        //        {
        //            if (this.direction == DirectionType.买)
        //            {
        //                tag -= GridBackwardInterval;
        //            }
        //            else
        //            {
        //                tag += GridBackwardInterval;
        //            }
        //        }
        //        if (dir1 == DirectionType.买)
        //        {
        //            dir1 = DirectionType.卖;
        //        }
        //        else
        //        {
        //            dir1 = DirectionType.买;
        //        }
        //        if (off1 == OffsetFlagType.开仓)
        //        {
        //           off1 = OffsetFlagType.平仓;
        //        }
        //        else
        //        {
        //            off1 = OffsetFlagType.开仓;
        //        }
        //        this.Log.WriteLine(MsgType.Warning, "发送纠错单！");
        //        return PriorityOrderLogic(volume, dir1, off1, tag);
        //    }
        //    //正常报单
        //    else if (volume > 0)
        //    {
        //        return PriorityOrderLogic(volume, dir1, off1,tag);
        //    }
        //    else throw new OperationCanceledException();
        //}
        void OnTimerCancel(object order)
        {
            if (order != null)
            {
                ((IStrategyOrder)order).Cancel();
                Log.WriteLine(((IStrategyOrder)order).Contract.InstrumentID + "撤单!");
            }
            else
            {
                Log.WriteLine(MsgType.Warning, "订单不存在，撤单失败!");
            }
        }
        void PriorityOrderLogic(int volume, DirectionType dir, OffsetFlagType off, object tag)
        {
             foreach (EachGridInfo Grid in saveData.GridList)
             {
                 if (Grid.tag == tag)
                 {
                     if (off == OffsetFlagType.开仓)
                     {
                         if (dir == DirectionType.买)
                         {
                             if (order2 == null)
                             {
                                 order2 = this.Positions[1].InputOrder(secondLegTick.BidPrice, volume, dir, off, tag);
                                 Grid.ForwardOrder.secondLegOrder = order2;
                                 if (Grid.secondLegVolume < EachSecondLegVol)
                                 {//以重新调整好的数量买入
                                     order1 = this.Positions[0].InputOrder(firstLegTick.AskPrice + firstPassPrice, (int)proportion * Grid.secondLegVolume, dir, off);
                                     Grid.ForwardOrder.firstLegOrder = order1;
                                 }
                                 else
                                 {
                                     order1 = this.Positions[0].InputOrder(firstLegTick.AskPrice + firstPassPrice, EachFirstLegVol, dir, off);
                                     Grid.ForwardOrder.firstLegOrder = order1;
                                 }

                             }


                         }
                         else//方向为卖
                         {
                             if (order2 == null)
                             {
                                 Grid.ForwardOrder.secondLegOrder = this.Positions[1].InputOrder(secondLegTick.AskPrice, EachSecondLegVol, dir, off, tag);
                                 if (Grid.secondLegVolume < EachSecondLegVol)
                                 {//以重新调整好的数量买入
                                     order1 = this.Positions[0].InputOrder(firstLegTick.BidPrice - firstPassPrice, (int)proportion * Grid.secondLegVolume, dir, off);
                                     Grid.ForwardOrder.firstLegOrder = order1;
                                 }
                                 else
                                 {
                                     order1 = this.Positions[0].InputOrder(firstLegTick.BidPrice - firstPassPrice, EachFirstLegVol, dir, off);
                                     Grid.ForwardOrder.firstLegOrder = order1;
                                 }
                             }
                         }
                     }
                     else
                     {
                         if (dir == DirectionType.买)
                         {
                             if (order2 == null)
                             {
                                 Grid.ForwardOrder.secondLegOrder = this.Positions[1].InputOrder(secondLegTick.BidPrice, EachSecondLegVol, dir, off, tag);
                                 if (Grid.secondLegVolume < EachSecondLegVol)
                                 {//以重新调整好的数量买入
                                     order1 = this.Positions[0].InputOrder(firstLegTick.AskPrice + firstPassPrice, (int)proportion * Grid.secondLegVolume, dir, off);
                                     Grid.ForwardOrder.firstLegOrder = order1;
                                 }
                                 else
                                 {
                                     order1 = this.Positions[0].InputOrder(firstLegTick.AskPrice + firstPassPrice, EachFirstLegVol, dir, off);
                                     Grid.ForwardOrder.firstLegOrder = order1;
                                 }
                             }
                         }
                         else
                         {
                             if (order2 == null)
                             {
                                 Grid.ForwardOrder.secondLegOrder = this.Positions[1].InputOrder(secondLegTick.AskPrice, EachSecondLegVol, dir, off, tag);
                                 if (Grid.secondLegVolume < EachSecondLegVol)
                                 {//以重新调整好的数量买入
                                     order1 = this.Positions[0].InputOrder(firstLegTick.BidPrice - firstPassPrice, (int)proportion * Grid.secondLegVolume, dir, off);
                                     Grid.ForwardOrder.firstLegOrder = order1;
                                 }
                                 else
                                 {
                                     order1 = this.Positions[0].InputOrder(firstLegTick.BidPrice - firstPassPrice, EachFirstLegVol, dir, off);
                                     Grid.ForwardOrder.firstLegOrder = order1;
                                 }
                             }
                         }
                     }
                 }
             }

        }
        int Calculate(double price)
        {
            if (isOpposite)
            {
                if (direction == DirectionType.买)
                {
                    int temp = (int)Math.Round(((upLine - price) / GridForwardInterval));
                    if (temp < 0)
                        temp = 0;
                    if (temp > (int)Math.Round((upLine - downLine) / GridForwardInterval))
                        temp = (int)Math.Round((upLine - downLine) / GridForwardInterval);
                    return temp;
                }
                else
                {
                    int temp = (int)Math.Round(((price - downLine) / GridForwardInterval));
                    if (temp < 0)
                        temp = 0;
                    if (temp > (int)Math.Round(((upLine - downLine) / GridForwardInterval)))
                        temp = (int)Math.Round(((upLine - downLine) / GridForwardInterval));
                    return temp;
                }
            }
            else
            {
                if (direction == DirectionType.买)
                {
                    int temp = (int)Math.Round(((upLine - price) / GridBackwardInterval));
                    if (temp < 0)
                        temp = 0;
                    if (temp > (int)Math.Round((upLine - downLine) / GridBackwardInterval))
                        temp = (int)Math.Round((upLine - downLine) / GridBackwardInterval);
                    return temp;
                }
                else
                {
                    int temp = (int)Math.Round(((price - downLine) / GridBackwardInterval));
                    if (temp < 0)
                        temp = 0;
                    if (temp > (int)Math.Round(((upLine - downLine) / GridBackwardInterval)))
                        temp = (int)Math.Round(((upLine - downLine) / GridBackwardInterval));
                    return temp;
                }
            }
        }
        protected override void OnSetConfig(StrategyConfig Config)
        {
            if (Config[5].IntValue == 0)
            {
                this.direction = DirectionType.买;

            }
            else if (Config[5].IntValue == 1)
            {
                this.direction = DirectionType.卖;
            }
            this.config = Config;
            this.firstLegTime = Config[0].IntValue;
            this.firstLegAddition = Config[1].IntValue;
            this.secondLegTime = Config[2].IntValue;
            this.secondLegAddition = Config[3].IntValue;
            this.upLine = Config[7].DoubleValue;
            this.downLine = Config[8].DoubleValue;
            this.GridForwardInterval = Config[9].DoubleValue;
            this.GridBackwardInterval = Config[10].DoubleValue;
            this.AdvanceOrder = Config[11].IntValue;
            this.EachFirstLegVol = Config[12].IntValue;
            this.EachSecondLegVol = Config[13].IntValue;
            this.firstPassPrice = Config[14].IntValue;
            this.firstDelayCancel = Config[15].IntValue;

            if (Config[6].IntValue == 0)
            {
                this.direction = DirectionType.买;

            }
            else if (Config[6].IntValue == 1)
            {
                this.direction = DirectionType.卖;
            }
            //判断开平
            if (Config[7].IntValue == 0)
            {
                this.statusFlag = OffsetFlagType.开仓;
            }
            else if (Config[7].IntValue == 1)
            {
                this.statusFlag = OffsetFlagType.平仓;
            }
            //if (saveData.SaveDir != direction || saveData.SaveOff != statusFlag || saveData.SaveupLine != upLine || saveData.SavedownLine != downLine || saveData.SaveGridForwardInterval != GridForwardInterval)
            //{
            //    LoadDefault();
            //}
            this.ForData = AdvanceOrder * this.Positions[0].Contract.Product.PriceTick;
            this.OppData = AdvanceOrder * this.Positions[0].Contract.Product.PriceTick;
        }
        void loadDefault()
        {
            if (statusFlag == OffsetFlagType.开仓)
            {
                //买入的情况
                if (direction == DirectionType.买)
                {
                    for (int i = 0; ; i++)
                    {
                        double temp = upLine - GridForwardInterval * i;
                        if (temp <= downLine)
                            break;

                        EachGridInfo grid = new EachGridInfo();
                        grid.tag = temp;
                        grid.Price = temp;
                        grid.TodayVolume = 0;
                        grid.totalVolume = 0;
                        grid.IsSetOrder = false;
                        saveData.GridList.Add(grid);
                    }
                }
                //卖出的情况
                else
                {
                    for (int i = 0; ; i++)
                    {
                        double temp = downLine + GridForwardInterval * i;
                        if (temp >= upLine)
                            break;

                        EachGridInfo grid = new EachGridInfo();
                        grid.Price = temp;
                        grid.tag = temp;
                        grid.TodayVolume = 0;
                        grid.totalVolume = 0;
                        grid.IsSetOrder = false;
                        saveData.GridList.Add(grid);
                    }
                }
            }
            //平仓的情况
            else
            {
                //买入的情况
                if (direction == DirectionType.买)
                {
                    for (int i = 0; ; i++)
                    {
                        double temp = upLine - GridForwardInterval * i;
                        if (temp <= downLine)
                            break;

                        EachGridInfo grid = new EachGridInfo();
                        grid.Price = temp;
                        grid.tag = temp;
                        grid.TodayVolume = EachFirstLegVol;
                        grid.totalVolume = 0;
                        grid.IsSetOrder = false;
                        saveData.GridList.Add(grid);
                    }
                }
                //卖出的情况
                else
                {
                    for (int i = 0; ; i++)
                    {
                        double temp = downLine + GridForwardInterval * i;
                        if (temp >= upLine)
                            break;

                        EachGridInfo grid = new EachGridInfo();
                        grid.Price = temp;
                        grid.tag = temp;
                        grid.TodayVolume = EachFirstLegVol;
                        grid.totalVolume = 0;
                        grid.IsSetOrder = false;
                        saveData.GridList.Add(grid);
                    }
                }
            }
        }
    }
}

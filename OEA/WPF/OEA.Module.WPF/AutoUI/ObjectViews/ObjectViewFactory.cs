/*******************************************************
 * 
 * 作者：胡庆访
 * 创建时间：20110215
 * 说明：ObjectView的工厂
 * 运行环境：.NET 4.0
 * 版本号：1.0.0
 * 
 * 历史记录：
 * 创建文件 胡庆访 20100215
 * 
*******************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using OEA.MetaModel;
using OEA.MetaModel.View;

using OEA.Module.WPF.Controls;
using OEA.Module.WPF.Editors;

using OEA.Module.WPF.ViewControllers;

namespace OEA.Module.WPF
{
    /// <summary>
    /// 通过元数据构造 ObjectView 的工厂类。
    /// </summary>
    public class ObjectViewFactory
    {
        private BlockUIFactory _uiFactory;

        public BlockUIFactory BlockUIFactory
        {
            get { return this._uiFactory; }
        }

        public ObjectViewFactory(BlockUIFactory uiFactory)
        {
            if (uiFactory == null) throw new ArgumentNullException("factory");

            this._uiFactory = uiFactory;
        }

        /// <summary>
        /// 根据 blockInfo 中的 BlockType 生成 ObjectView。
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public WPFObjectView CreateView(Block block)
        {
            //如果是自定义视图，则自成该类的对象
            if (block.CustomViewType != null)
            {
                var viewType = Type.GetType(block.CustomViewType);
                var view = Activator.CreateInstance(viewType, block.EVM) as WPFObjectView;
                return view;
            }

            //环绕块
            var sur = block as SurrounderBlock;
            if (sur != null)
            {
                if (sur.SurrounderType == SurrounderType.Navigation)
                {
                    return this.CreateNavigateQueryObjectView(block.EVM);
                }
                return this.CreateConditionQueryObjectView(block.EVM);
            }

            if (block.BlockType == BlockType.List)
            {
                return this.CreateListObjectView(block.EVM);
            }

            return this.CreateDetailObjectView(block.EVM);
        }

        /// <summary>
        /// 为某个实体类型生成逻辑视图。
        /// </summary>
        /// <param name="entityType">实体类型</param>
        /// <returns></returns>
        public ListObjectView CreateListObjectView(Type entityType)
        {
            var evm = UIModel.Views.CreateDefaultView(entityType);

            return this.CreateListObjectView(evm);
        }

        /// <summary>
        /// 为某个实体类型生成逻辑视图。
        /// </summary>
        /// <param name="entityViewInfo">实体类的视图元数据</param>
        /// <returns></returns>
        public ListObjectView CreateListObjectView(EntityViewMeta entityViewInfo, bool isLookup = false)
        {
            var view = new ListObjectView(entityViewInfo);

            if (isLookup) { view.ShowInWhere = ShowInWhere.Lookup; }

            this.InitListView(view);

            this.OnViewCreated(view);

            return view;
        }

        /// <summary>
        /// 为某实体类构建一个详细面板视图
        /// </summary>
        /// <param name="entityViewInfo"></param>
        /// <returns></returns>
        public DetailObjectView CreateDetailObjectView(EntityViewMeta entityViewInfo)
        {
            var view = new DetailObjectView(entityViewInfo);

            this.InitDetailView(view);

            this.OnViewCreated(view);

            return view;
        }

        /// <summary>
        /// 为某实体类构建一个导航查询面板视图
        /// </summary>
        /// <param name="entityViewInfo"></param>
        /// <returns></returns>
        public NavigateQueryObjectView CreateNavigateQueryObjectView(EntityViewMeta entityViewInfo)
        {
            var view = new NavigateQueryObjectView(entityViewInfo);

            this.InitDetailView(view);

            view.AttachNewQueryObject();

            this.OnViewCreated(view);

            return view;
        }

        /// <summary>
        /// 为某实体类构建一个条件查询面板视图
        /// </summary>
        /// <param name="entityViewInfo"></param>
        /// <returns></returns>
        public ConditionQueryObjectView CreateConditionQueryObjectView(EntityViewMeta entityViewInfo)
        {
            var view = new ConditionQueryObjectView(entityViewInfo);

            this.InitDetailView(view);

            view.AttachNewQueryObject();

            this.OnViewCreated(view);

            return view;
        }

        private void InitListView(ListObjectView view)
        {
            //如果是选择视图，则应该使用显示模型来创建控件。
            var resultControl = this._uiFactory.CreateTreeListControl(view.Meta, view.ShowInWhere);

            //为 ListObjectView 初始化 ListEditor
            var listEditor = new MTTGListEditor(view);
            listEditor.SetControl(resultControl);
            view.InitializeEditor(listEditor);

            //ListViewController
            view.DataLoader = new ListViewController(view);
        }

        private void InitDetailView(DetailObjectView view)
        {
            var control = this._uiFactory.CreateDetailPanel(view);
            view.SetControl(control);

            //DetailViewController
            view.DataLoader = new DetailViewController(view);
        }

        public event EventHandler<InstanceCreatedEventArgs<ObjectView>> ViewCreated;

        private void OnViewCreated(ObjectView view)
        {
            var handler = this.ViewCreated;
            if (handler != null) handler(this, new InstanceCreatedEventArgs<ObjectView>(view));
        }
    }
}
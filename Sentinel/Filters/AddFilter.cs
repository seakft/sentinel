#region License
//
// � Copyright Ray Hayes
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.
//
#endregion

#region Using directives

using System.Linq;
using System.Windows;
using Sentinel.Controls;
using Sentinel.Logger;
using Sentinel.Services;
using Sentinel.Support;

#endregion

namespace Sentinel.Filters
{

    #region Using directives

    #endregion

    public class AddFilter : IAddFilterService
    {
        #region IAddFilterService Members

        public void Add()
        {
            AddEditFilterWindow filterWindow = new AddEditFilterWindow();
            using (AddEditFilter data = new AddEditFilter(filterWindow, false))
            {
                filterWindow.DataContext = data;
                filterWindow.Owner = Application.Current.MainWindow;
                bool? dialogResult = filterWindow.ShowDialog();
                if (dialogResult != null && (bool) dialogResult)
                {
                    Filter filter = Construct(data);
                    if (filter != null)
                    {
                        IFilteringService service = ServiceLocator.Instance.Get<IFilteringService>();
                        if (service != null)
                        {
                            service.Filters.Add(filter);
                        }
                    }
                }
            }
        }

        #endregion

        private static Filter Construct(AddEditFilter data)
        {
            Filter filter = null;

            if (data.FieldIndex >= 0 && data.FieldIndex < data.Fields.Count())
            {
                LogEntryField fieldToMatch =
                    LogEntryFieldHelper.FieldNameToEnumeration(data.Fields.ElementAt(data.FieldIndex));
                MatchMode mode = MatchModeConverter.ConvertFrom(data.FilterMethods.ElementAt(data.FilterMethod));

                filter = new Filter
                             {
                                 Name = data.Name,
                                 Field = fieldToMatch,
                                 Mode = mode,
                                 Pattern = data.Pattern,
                                 Enabled = true
                             };
            }

            return filter;
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ServiceStackVS
{
    public static class VSIXUtils
    {
        public static bool IsSingleProjectItemSelection(out IVsHierarchy hierarchy, out uint itemid)
        {
            hierarchy = null;
            itemid = VSConstants.VSITEMID_NIL;
            int hr = VSConstants.S_OK;
            var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            if (monitorSelection == null || solution == null)
            {
                return false;
            }
            IVsMultiItemSelect multiItemSelect = null;
            IntPtr hierarchyPtr = IntPtr.Zero;
            IntPtr selectionContainerPtr = IntPtr.Zero;
            try
            {
                hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect, out selectionContainerPtr);
                if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemid == VSConstants.VSITEMID_NIL)
                {
                    // there is no selection
                    return false;
                }
                // multiple items are selected
                if (multiItemSelect != null) return false;
                // there is a hierarchy root node selected, thus it is not a single item inside a project
                if (itemid == VSConstants.VSITEMID_ROOT) return false;
                hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                if (hierarchy == null) return false;
                Guid guidProjectID = Guid.Empty;
                if (ErrorHandler.Failed(solution.GetGuidOfProject(hierarchy, out guidProjectID)))
                {
                    return false; // hierarchy is not a project inside the Solution if it does not have a ProjectID Guid
                }
                // if we got this far then there is a single project item selected
                return true;
            }
            finally
            {
                if (selectionContainerPtr != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainerPtr);
                }
                if (hierarchyPtr != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPtr);
                }
            }
        }

        /// <summary>
        /// Gets the selected project.
        /// From http://uisurumadushanka89.blogspot.com.au/2013/04/visual-studio-extensibility-get-active.html
        /// </summary>
        /// <returns></returns>
        public static Project GetSelectedProject()
        {
            return GetSelectObject<Project>();
        }

        public static T GetSelectObject<T>() where T : class
        {
            IntPtr hierarchyPointer, selectionContainerPointer;
            Object selectedObject = null;
            IVsMultiItemSelect multiItemSelect;
            uint projectItemId;

            IVsMonitorSelection monitorSelection =
                    (IVsMonitorSelection)Package.GetGlobalService(
                    typeof(SVsShellMonitorSelection));

            monitorSelection.GetCurrentSelection(out hierarchyPointer,
                                                 out projectItemId,
                                                 out multiItemSelect,
                                                 out selectionContainerPointer);

            IVsHierarchy selectedHierarchy = null;
            try
            {
                selectedHierarchy = Marshal.GetTypedObjectForIUnknown(
                                                     hierarchyPointer,
                                                     typeof(IVsHierarchy)) as IVsHierarchy;
            }
            catch (Exception)
            {
                return null;
            }

            if (selectedHierarchy != null)
            {
                ErrorHandler.ThrowOnFailure(selectedHierarchy.GetProperty(
                                                  projectItemId,
                                                  (int)__VSHPROPID.VSHPROPID_ExtObject,
                                                  out selectedObject));
            }

            return selectedObject as T;
        }
    }

    public static class VsHelperGuids
    {
        public const string CSharpProjectKind = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
        public const string FSharpProjectKind = "{F2A71F9B-5D33-465A-A702-920D77279786}";
        public const string VbNetProjectKind = "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}";

        public const string ProjectUnloaded = "{67294A52-A4F0-11D2-AA88-00C04F688DDE}";

    }
}

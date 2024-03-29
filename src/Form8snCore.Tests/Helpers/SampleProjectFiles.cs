﻿using System.Collections.Generic;
using Form8snCore.FileFormats;

// ReSharper disable ArrangeAccessorOwnerBody
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global

namespace Form8snCore.Tests.Helpers
{
    public static class SampleProjectFiles
    {
        public static TemplateProject BasicFile
        {
            get
            {
                return new TemplateProject("Sample File")
                {
                    Notes = "Project notes",
                    Pages = BasicPages,
                    Version = 0,
                    DataFilters = BasicDocFilters,
                    FontName = null,
                    BaseFontSize = null,
                    BasePdfFile = "SamplePdfFile",
                    SampleFileName = null
                };
            }
        }

        public const string DocumentFilterKey = "Doc Filter to be referenced";
        public static Dictionary<string, MappingInfo> BasicDocFilters
        {
            get
            {
                return new Dictionary<string, MappingInfo>
                {
                    {
                        DocumentFilterKey, new MappingInfo
                        {
                            DataPath = new[] { "", "Reclaims", "[0]", "Amounts", "ReclaimAmount" },
                            MappingType = MappingType.Total
                        }
                    },
                    {
                        "DocFilter 2 - picker", new MappingInfo
                        {
                            MappingType = MappingType.IfElse,
                            DataPath = new[] { "", "Claimant", "EntityType" },
                            MappingParameters = new Dictionary<string, string>
                            {
                                { nameof(IfElseMappingParams.ExpectedValue), "Bank" },
                                { nameof(IfElseMappingParams.Same), ".Branch" },
                                { nameof(IfElseMappingParams.Different), ".BatchNumber" }
                            }
                        }
                    },
                    {
                        "HugeGroup", new MappingInfo // split into N, with a large enough N that we only get one item
                        {
                            DataPath = new[] { "", "Reclaims" },
                            MappingType = MappingType.SplitIntoN,
                            MappingParameters = new Dictionary<string, string>
                            {
                                { nameof(MaxCountMappingParams.MaxCount), "100" }
                            }
                        }
                    }
                };
            }
        }

        public static List<TemplatePage> BasicPages
        {
            get
            {
                return new List<TemplatePage>
                {
                    new TemplatePage
                    {
                        Boxes = Basic_Page_Boxes,
                        Name = "Basic page 1 of 2",
                        Notes = "Page 1 notes",
                        BackgroundImage = null,
                        HeightMillimetres = A4_Height,
                        WidthMillimetres = A4_Width,
                        RenderBackground = true,
                        RepeatMode = new RepeatMode
                        {
                            Repeats = false,
                            DataPath = null
                        },
                        PageDataFilters = Basic_Page1_Filters,
                        PageFontSize = null
                    },
                    new TemplatePage
                    {
                        Boxes = Basic_Page_Boxes,
                        Name = "Basic page 1 of 2",
                        Notes = "Page 2 notes",
                        BackgroundImage = null,
                        HeightMillimetres = A4_Height,
                        WidthMillimetres = A4_Width,
                        RenderBackground = true,
                        RepeatMode = new RepeatMode
                        {
                            Repeats = false,
                            DataPath = null
                        },
                        PageDataFilters = Basic_Page2_Filters,
                        PageFontSize = null
                    }
                };
            }
        }

        public const string Page1FilterKey = "Page filter 1";
        public static Dictionary<string, MappingInfo> Basic_Page1_Filters
        {
            get
            {
                return new Dictionary<string, MappingInfo>
                {
                    {
                        Page1FilterKey, new MappingInfo
                        {
                            DataPath = new[] { "#", DocumentFilterKey },
                            MappingType = MappingType.Join,
                            MappingParameters = new Dictionary<string, string>{
                                {nameof(JoinPathsMappingParams.Infix), "-"},
                                {nameof(JoinPathsMappingParams.ExtraData), ".AuthorisedRepresentative.CompanyName"}
                            }
                        }
                    }
                };
            }
        }
        
        public const string Page2FilterKey = "Page filter 2";
        public static Dictionary<string, MappingInfo> Basic_Page2_Filters
        {
            get
            {
                return new Dictionary<string, MappingInfo>
                {
                    {
                        Page2FilterKey, new MappingInfo
                        {
                            DataPath = new[] { "#", DocumentFilterKey },
                            MappingType = MappingType.Join,
                            MappingParameters = new Dictionary<string, string>{
                                {nameof(JoinPathsMappingParams.Infix), "!!"},
                                {nameof(JoinPathsMappingParams.ExtraData), ".AuthorisedRepresentative.CompanyName"}
                            }
                        }
                    }
                };
            }
        }

        public static IDictionary<string, TemplateBox> Basic_Page_Boxes
        {
            get
            {
                return new Dictionary<string, TemplateBox>
                {
                    {
                        "Box 1", new TemplateBox
                        {
                            Alignment = TextAlignment.MidlineCentre,
                            Left = 10, Top = 50,
                            Height = 20, Width = 100,
                            Notes = "notes for box ",
                            BoxOrder = null,
                            DependsOn = null,
                            DisplayFormat = null,
                            IsRequired = false,
                            MappingPath = new[] { "", "Claimant", "Name" },
                            WrapText = false,
                            ShrinkToFit = true,
                            BoxFontSize = null
                        }
                    },
                    {
                        "Box 2", new TemplateBox
                        {
                            Alignment = TextAlignment.MidlineCentre,
                            Left = 10, Top = 250,
                            Height = 25, Width = 150,
                            Notes = "notes for box 2",
                            BoxOrder = null,
                            DependsOn = null,
                            DisplayFormat = null,
                            IsRequired = false,
                            MappingPath = new[] { "", "Claimant", "MailingAddress", "Country", "Code" },
                            WrapText = true,
                            ShrinkToFit = false,
                            BoxFontSize = null
                        }
                    },
                    {
                        "BoxWithDocRef", new TemplateBox
                        {
                            Alignment = TextAlignment.MidlineCentre,
                            Left = 10, Top = 250,
                            Height = 25, Width = 150,
                            Notes = "notes for box 2",
                            BoxOrder = null,
                            DependsOn = null,
                            DisplayFormat = null,
                            IsRequired = false,
                            MappingPath = new[] { "#", DocumentFilterKey },
                            WrapText = true,
                            ShrinkToFit = false,
                            BoxFontSize = null
                        }
                    },
                    {
                        "BoxWithPageRef", new TemplateBox
                        {
                            Alignment = TextAlignment.MidlineCentre,
                            Left = 10, Top = 250,
                            Height = 25, Width = 150,
                            Notes = "notes for box 2",
                            BoxOrder = null,
                            DependsOn = null,
                            DisplayFormat = null,
                            IsRequired = false,
                            MappingPath = new[] { "#", Page1FilterKey },
                            WrapText = true,
                            ShrinkToFit = false,
                            BoxFontSize = null
                        }
                    },
                    {
                        "BoxWithDirectData", new TemplateBox
                        {
                            Alignment = TextAlignment.MidlineCentre,
                            Left = 10, Top = 250,
                            Height = 25, Width = 150,
                            IsRequired = false,
                            MappingPath = new[] { "!", "This is some data", " And it should go straight in", "" },
                            WrapText = true,
                            ShrinkToFit = false
                        }
                    }
                };
            }
        }

        private const double A4_Width = 210.0;
        private const double A4_Height = 297.0;
    }
}
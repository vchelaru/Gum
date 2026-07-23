/** Shared JSON shapes for the Chromium box tree → Gum pipeline. */

export type Rect = {
  x: number;
  y: number;
  width: number;
  height: number;
};

/** Computed + authored style fields collected by extract.ts. */
export type BoxStyle = {
  display: string;
  backgroundImage: string;
  backgroundSize: string;
  objectFit: string;
  objectPosition: string;
  listStyleType: string;
  flexDirection: string;
  flexWrap: string;
  rowGap: number;
  columnGap: number;
  flexGrow: number;
  order: number;
  alignItems: string;
  alignSelf: string;
  justifyContent: string;
  textAlign: string;
  paddingTop: number;
  paddingRight: number;
  paddingBottom: number;
  paddingLeft: number;
  marginTop?: number;
  marginRight?: number;
  marginBottom?: number;
  marginLeft?: number;
  zIndex: number;
  gridTemplateColumns: string;
  gridTemplateRows: string;
  gridAutoFlow: string;
  gridColumnStart: string;
  gridColumnEnd: string;
  gridRowStart: string;
  gridRowEnd: string;
  gridColumnStartSpecified: string;
  gridColumnEndSpecified: string;
  gridRowStartSpecified: string;
  gridRowEndSpecified: string;
  gridAreaSpecified: string;
  gridColumnSpecified: string;
  gridRowSpecified: string;
  position: string;
  backgroundColor: string;
  borderTopLeftRadius: number;
  borderTopWidth: number;
  borderRightWidth: number;
  borderBottomWidth: number;
  borderLeftWidth: number;
  borderTopColor: string;
  borderRightColor: string;
  borderBottomColor: string;
  borderLeftColor: string;
  boxShadow: string;
  textShadow: string;
  webkitTextStrokeWidth: number;
  overflow: string;
  opacity: number;
  filter: string;
  needsRaster: boolean;
  rasterWholeSubtree: boolean;
  /** When true, convert screenshots with omitBackground (SVG / pseudo icons). */
  rasterOmitBackground?: boolean;
  color: string;
  fontSize: number;
  fontWeight: string;
  fontStyle: string;
  fontFamily: string;
  widthSpecified: string;
  heightSpecified: string;
  borderImageSource: string;
  borderImageSlice: number;
  borderImageRepeat: string;
};

export type BoxNode = {
  id: string | null;
  tag: string;
  rect: Rect;
  text: string;
  lineCount: number;
  imgSrc: string | null;
  naturalWidth: number;
  naturalHeight: number;
  /** Set by convert.ts after rasterizeEffects (key into assetMap). */
  rasterSrc: string | null;
  style: BoxStyle;
  children: BoxNode[];
};

export type Color = { r: number; g: number; b: number; a: number };

export type ResponsiveAxis = {
  units: 'Absolute' | 'PercentageOfParent' | 'RelativeToParent';
  value: number;
  ambiguous?: boolean;
};

export type ResponsiveEntry = {
  width: ResponsiveAxis;
  height: ResponsiveAxis;
};

export type ResponsiveMap = Map<string, ResponsiveEntry>;

export type ViewportSize = { width: number; height: number };

export type NineSliceInfo = {
  sourceFile: string;
  frameWidth: number;
  tiling?: boolean;
};

export type GusxInstance = {
  name: string;
  baseType: string;
};

/** Compact variable record written into .gusx by toGusx. */
export type GusxVariable = {
  type: string;
  name: string;
  valueType: string;
  value: string | number | boolean;
};

export type MappedScreen = {
  instances: GusxInstance[];
  variables: GusxVariable[];
  warnings: string[];
};

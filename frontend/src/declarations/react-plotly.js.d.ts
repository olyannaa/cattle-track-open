/* eslint-disable @typescript-eslint/no-explicit-any */
// src/types/react-plotly.js.d.ts
declare module 'react-plotly.js' {
    import { Component } from 'react';
    import { Layout, Data, Config } from 'plotly.js';

    export interface PlotParams {
        data: Data[];
        layout?: Partial<Layout>;
        config?: Partial<Config>;
        style?: React.CSSProperties;
        useResizeHandler?: boolean;
        className?: string;
        onInitialized?: (figure: any, graphDiv: any) => void;
        onUpdate?: (figure: any, graphDiv: any) => void;
    }

    export default class Plot extends Component<PlotParams> {}
}

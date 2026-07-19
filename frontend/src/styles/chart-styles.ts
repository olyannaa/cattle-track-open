import { LineConfig } from '@ant-design/plots';

export const chartStyles: LineConfig = {
    padding: 'auto',
    paddingRight: 15,
    axis: {
        x: {
            position: 'bottom',
            grid: true,
            gridLineWidth: 1,
            gridStroke: 'rgb(0, 0, 26,0.15)',
            gridStrokeOpacity: 1,
            line: true,
            lineLineWidth: 1,
            lineStroke: 'rgb(0, 0, 26,1)',
            label: true,
            labelAlign: 'horizontal',
            size: 20,
        },
        y: {
            position: 'left',
            grid: true,
            gridLineWidth: 1,
            gridStroke: 'rgb(0, 0, 26,0.15)',
            gridStrokeOpacity: 1,
            label: true,
        },
    },
    line: {
        style: {
            stroke: '#FF4218',
            strokeWidth: 8,
        },
    },
    area: {
        style: {
            fill: 'linear-gradient(90deg, rgba(255, 66, 24, 0.3) 0%, rgba(255, 66, 24, 0.05) 100%)',
        },
    },
};

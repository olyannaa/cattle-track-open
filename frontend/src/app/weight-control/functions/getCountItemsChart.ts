export const getCountItemsChart = (name: string, width: number) => {
    if (name === 'date') {
        if (width > 850) {
            return 9;
        } else if (width > 700) {
            return 8;
        } else if (width > 600) {
            return 7;
        } else if (width > 520) {
            return 6;
        } else if (width > 400) {
            return 5;
        } else if (width > 360) {
            return 4;
        } else {
            return 3;
        }
    } else if (name === 'age') {
        if (width > 830) {
            return 13;
        } else if (width > 700) {
            return 10;
        } else if (width > 600) {
            return 7;
        } else if (width > 400) {
            return 6;
        } else {
            return 5;
        }
    } else {
        if (width > 1117) {
            return 6;
        } else if (width > 850) {
            return 9;
        } else if (width > 700) {
            return 8;
        } else if (width > 600) {
            return 7;
        } else if (width > 520) {
            return 6;
        } else if (width > 400) {
            return 5;
        } else if (width > 360) {
            return 4;
        } else {
            return 3;
        }
    }
};

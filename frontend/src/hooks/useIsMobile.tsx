import { useWindowSize } from './useWindowSize';

const MOBILE_BREAKPOINT = 768;

export function useIsMobile(): boolean {
    const width = useWindowSize();
    return width < MOBILE_BREAKPOINT;
}

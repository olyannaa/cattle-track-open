export const getSiderStyle = (isMobile: boolean, collapsed: boolean): React.CSSProperties => {
    const headerHeight = 64;

    if (isMobile) {
        return {
            position: 'fixed',
            top: headerHeight,
            left: 0,
            height: `calc(100vh - ${headerHeight}px)`,
            width: 220,
            background: '#fff',
            boxShadow: '2px 0 8px rgba(0,0,0,0.15)',
            zIndex: 1000,
            transition: 'transform 0.3s ease',
            transform: collapsed ? 'translateX(-100%)' : 'translateX(0)',
        };
    }

    return {
        position: 'sticky',
        insetInlineStart: 0,
        top: 0,
        bottom: 0,
        height: `calc(100vh - ${headerHeight}px)`,
        minHeight: `calc(100vh - ${headerHeight}px)`,
        alignSelf: 'stretch',
        boxShadow: '0px 2px 8px rgba(0, 0, 0, 0.2)',
        background: '#ffffff',
        overflow: 'visible',
        zIndex: 1,
    };
};

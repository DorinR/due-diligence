import {} from '@radix-ui/react-icons';
import React from 'react';
import { cn } from '../../../lib/utils';

// Import and use the exact type signature from Radix UI icons
interface IconProps extends React.SVGAttributes<SVGElement> {
    children?: never;
    color?: string;
}

type RadixIconComponent = React.ForwardRefExoticComponent<
    IconProps & React.RefAttributes<SVGSVGElement>
>;

export type ButtonVariant = 'primary' | 'neutral' | 'danger' | 'soft';

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
    variant?: ButtonVariant;
    icon?: RadixIconComponent;
    iconPosition?: 'left' | 'right';
}

export const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
    (
        { className, variant = 'primary', icon: Icon, iconPosition = 'left', children, ...props },
        ref
    ) => {
        const baseStyles =
            'inline-flex items-center justify-center rounded-lg px-4 py-1 text-sm font-medium transition-colors focus:outline-none disabled:pointer-events-none disabled:opacity-50 cursor-pointer hover:bg-gray-400';

        const variantStyles = {
            primary:
                'bg-blue-600 text-white hover:bg-blue-700 focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
            neutral:
                'bg-gray-200 text-gray-900 hover:bg-gray-300 focus:ring-2 focus:ring-gray-200 focus:ring-offset-2 dark:bg-gray-700 dark:text-gray-100 dark:hover:bg-gray-600',
            danger: 'bg-red-600 text-white hover:bg-red-700 focus:ring-2 focus:ring-red-500 focus:ring-offset-2',
            soft: 'bg-transparent text-gray-500 hover:bg-gray-200 focus:ring-0 dark:text-gray-300 dark:hover:bg-gray-800 px-2 py-1',
        };

        const animations = 'transition-colors duration-400 ease-in-out';

        return (
            <button
                className={cn(baseStyles, variantStyles[variant], className, animations)}
                ref={ref}
                {...props}
            >
                {Icon && iconPosition === 'left' && (
                    <Icon className={cn('h-3 w-3', children && 'mr-2')} />
                )}
                {children}
                {Icon && iconPosition === 'right' && (
                    <Icon className={cn('h-3 w-3', children && 'ml-2')} />
                )}
            </button>
        );
    }
);

Button.displayName = 'Button';

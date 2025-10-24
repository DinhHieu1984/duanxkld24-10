-- NhanViet Database Initialization Script
-- This script sets up the initial database structure for NhanViet Labor Export Website

-- Create database if not exists (PostgreSQL)
-- Note: This is handled by docker-compose environment variables

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Create schemas
CREATE SCHEMA IF NOT EXISTS nhanviet;

-- Set default schema
SET search_path TO nhanviet, public;

-- Create initial tables for OrchardCore
-- Note: OrchardCore will create its own tables through migrations
-- This script is for any custom database setup

-- Create audit log table
CREATE TABLE IF NOT EXISTS audit_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id VARCHAR(255),
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(100) NOT NULL,
    entity_id VARCHAR(255),
    old_values JSONB,
    new_values JSONB,
    ip_address INET,
    user_agent TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_audit_logs_user_id ON audit_logs(user_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_created_at ON audit_logs(created_at);
CREATE INDEX IF NOT EXISTS idx_audit_logs_entity_type ON audit_logs(entity_type);

-- Create application settings table
CREATE TABLE IF NOT EXISTS app_settings (
    key VARCHAR(255) PRIMARY KEY,
    value TEXT,
    description TEXT,
    is_encrypted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Insert default settings
INSERT INTO app_settings (key, value, description) VALUES
('site_name', 'NhanViet Group', 'Website name'),
('site_description', 'Labor Export Services', 'Website description'),
('contact_email', 'info@nhanvietgroup.com', 'Contact email'),
('contact_phone', '+84-xxx-xxx-xxx', 'Contact phone'),
('timezone', 'Asia/Ho_Chi_Minh', 'Default timezone')
ON CONFLICT (key) DO NOTHING;

-- Create function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create trigger for app_settings
CREATE TRIGGER update_app_settings_updated_at 
    BEFORE UPDATE ON app_settings 
    FOR EACH ROW 
    EXECUTE FUNCTION update_updated_at_column();

-- Grant permissions
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA nhanviet TO nhanviet_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA nhanviet TO nhanviet_user;
GRANT USAGE ON SCHEMA nhanviet TO nhanviet_user;
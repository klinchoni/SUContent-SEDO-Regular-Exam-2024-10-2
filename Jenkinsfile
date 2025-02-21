pipeline {
    agent any

    environment {
        DOTNET_VERSION = '6.0.x'
    }

    stages {
        stage('Restore Dependencies') {
            steps {
                bat 'dotnet restore'
            }
        }
        
        stage('Build') {
            steps {
                bat 'dotnet build --no-restore'
            }
        }
        
        stage('Run Tests') {
            steps {
                bat 'dotnet test  --no-build --verbosity normal'
            }
        }
    }
}
